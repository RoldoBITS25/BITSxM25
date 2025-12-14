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

// Helper wrapper for deserializing books array
[Serializable]
public class BooksWrapper
{
    public Book[] books;
}

[Serializable]
public class EnigmaData
{
    public string word;
    public string enigma;
    public Book[] books; // Changed from List<Book> to Book[] for JsonUtility compatibility
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
        
        // Automatically fetch enigma data when manager is created
        Debug.Log("[EnigmaManager] Awake - Auto-fetching enigma data...");
        FetchEnigma("easy", "general", 
            onSuccess: (data) => {
                Debug.Log($"[EnigmaManager] Auto-fetch successful! Word: {data.word}, Books: {data.books?.Length ?? 0}");
            },
            onError: (error) => {
                Debug.LogError($"[EnigmaManager] Auto-fetch failed: {error}");
            });
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
        Debug.Log($"[EnigmaManager] Fetching enigma from: {url}");
        
        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    string jsonResponse = request.downloadHandler.text;
                    Debug.Log($"[EnigmaManager] Raw JSON response: {jsonResponse}");
                    
                    currentEnigma = JsonUtility.FromJson<EnigmaData>(jsonResponse);
                    
                    Debug.Log($"[EnigmaManager] Enigma fetched successfully: {currentEnigma.word}");
                    Debug.Log($"[EnigmaManager] Enigma text: {currentEnigma.enigma}");
                    Debug.Log($"[EnigmaManager] Number of books: {currentEnigma.books?.Length ?? 0}");
                    
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

        return System.Array.Find(currentEnigma.books, book => book.is_answer);
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

        if (index < 0 || index >= currentEnigma.books.Length)
        {
            Debug.LogWarning($"Book index {index} is out of range. Available books: {currentEnigma.books.Length}");
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

        return currentEnigma.books.Length;
    }

    /// <summary>
    /// Gets the enigma text (the note/clue)
    /// </summary>
    /// <returns>The enigma text or null if no enigma is loaded</returns>
    public string GetEnigmaText()
    {
        return currentEnigma?.enigma;
    }

    /// <summary>
    /// Gets the title of a book by its index
    /// </summary>
    /// <param name="index">The index of the book (0-4)</param>
    /// <returns>The book title or null if index is out of range</returns>
    public string GetBookTitle(int index)
    {
        Book book = GetBookByIndex(index);
        return book?.title;
    }

    /// <summary>
    /// Gets the content of a book by its index
    /// </summary>
    /// <param name="index">The index of the book (0-4)</param>
    /// <returns>The book content or null if index is out of range</returns>
    public string GetBookContent(int index)
    {
        Book book = GetBookByIndex(index);
        return book?.content;
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
