mergeInto(LibraryManager.library, {
  SendToBrowser: function (str) {
    var message = UTF8ToString(str);
    // Call the global function defined in src/main.js
    if (window.receiveFromUnity) {
        window.receiveFromUnity(message);
    } else {
        console.warn("window.receiveFromUnity is not defined");
    }
  },
});
