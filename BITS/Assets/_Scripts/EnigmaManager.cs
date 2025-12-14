using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class Book
{
    public string title;
    public string content;
    public bool is_answer;
}

[Serializable]
public class EnigmaData
{
    public string word;
    public string enigma;
    public List<Book> books;
    public string difficulty;
    public string category;
}

public class EnigmaManager : MonoBehaviour
{
    private static EnigmaManager _instance;
    public static EnigmaManager Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("EnigmaManager");
                _instance = go.AddComponent<EnigmaManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private EnigmaData currentEnigma;
    private const string BASE_URL = "http://localhost:8000/api/enigma/generate";

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// Fetches a new enigma from the API with the specified difficulty and category
    /// </summary>
    /// <param name="difficulty">Difficulty level (e.g., "easy", "medium", "hard")</param>
    /// <param name="category">Category (e.g., "science", "history", etc.)</param>
    /// <param name="onSuccess">Callback invoked when data is successfully fetched</param>
    /// <param name="onError">Callback invoked when an error occurs</param>
    public void FetchEnigma(string difficulty, string category, Action<EnigmaData> onSuccess = null, Action<string> onError = null)
    {
        StartCoroutine(FetchEnigmaCoroutine(difficulty, category, onSuccess, onError));
    }

    private IEnumerator FetchEnigmaCoroutine(string difficulty, string category, Action<EnigmaData> onSuccess, Action<string> onError)
    {
        string url = $"{BASE_URL}?difficulty={difficulty}&category={category}";
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string jsonResponse = request.downloadHandler.text;
                    currentEnigma = JsonUtility.FromJson<EnigmaData>(jsonResponse);
                    
                    Debug.Log($"Enigma fetched successfully: {currentEnigma.word}");
                    onSuccess?.Invoke(currentEnigma);
                }
                catch (Exception e)
                {
                    string errorMsg = $"Failed to parse enigma data: {e.Message}";
                    Debug.LogError(errorMsg);
                    onError?.Invoke(errorMsg);
                }
            }
            else
            {
                string errorMsg = $"Failed to fetch enigma: {request.error}";
                Debug.LogError(errorMsg);
                onError?.Invoke(errorMsg);
            }
        }
    }

    /// <summary>
    /// Checks if the provided answer matches the correct word
    /// </summary>
    /// <param name="answer">The user's answer</param>
    /// <returns>True if the answer is correct, false otherwise</returns>
    public bool CheckAnswer(string answer)
    {
        if (currentEnigma == null)
        {
            Debug.LogWarning("No enigma data loaded. Cannot check answer.");
            return false;
        }

        // Case-insensitive comparison, trimming whitespace
        bool isCorrect = string.Equals(
            answer.Trim(), 
            currentEnigma.word.Trim(), 
            StringComparison.OrdinalIgnoreCase
        );

        Debug.Log($"Answer '{answer}' is {(isCorrect ? "CORRECT" : "INCORRECT")}. Expected: '{currentEnigma.word}'");
        return isCorrect;
    }

    /// <summary>
    /// Gets the current enigma data
    /// </summary>
    /// <returns>The current EnigmaData or null if none is loaded</returns>
    public EnigmaData GetCurrentEnigma()
    {
        return currentEnigma;
    }

    /// <summary>
    /// Gets the correct answer for the current enigma
    /// </summary>
    /// <returns>The correct word or null if no enigma is loaded</returns>
    public string GetCorrectAnswer()
    {
        return currentEnigma?.word;
    }

    /// <summary>
    /// Gets the book that contains the answer
    /// </summary>
    /// <returns>The book marked as the answer, or null if none found</returns>
    public Book GetAnswerBook()
    {
        if (currentEnigma == null || currentEnigma.books == null)
            return null;

        return currentEnigma.books.Find(book => book.is_answer);
    }

    /// <summary>
    /// Gets a book by its index in the books list
    /// </summary>
    /// <param name="index">The index of the book to retrieve</param>
    /// <returns>The book at the specified index, or null if index is out of range</returns>
    public Book GetBookByIndex(int index)
    {
        if (currentEnigma == null || currentEnigma.books == null)
        {
            Debug.LogWarning("No enigma data loaded. Cannot get book by index.");
            return null;
        }

        if (index < 0 || index >= currentEnigma.books.Count)
        {
            Debug.LogWarning($"Book index {index} is out of range. Available books: {currentEnigma.books.Count}");
            return null;
        }

        return currentEnigma.books[index];
    }

    /// <summary>
    /// Gets the total number of books in the current enigma
    /// </summary>
    /// <returns>The number of books, or 0 if no enigma is loaded</returns>
    public int GetBookCount()
    {
        if (currentEnigma == null || currentEnigma.books == null)
            return 0;

        return currentEnigma.books.Count;
    }

    /// <summary>
    /// Clears the current enigma data
    /// </summary>
    public void ClearEnigma()
    {
        currentEnigma = null;
        Debug.Log("Enigma data cleared");
    }
}
