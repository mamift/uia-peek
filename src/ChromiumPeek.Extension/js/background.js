// background.js (MV3 service worker)

// Simple startup log so we know the extension is alive.
chrome.runtime.onInstalled.addListener(() => {
    console.log("[ChromiumPeek] Extension installed.");
});

// ONLY if you really want to handle clicks directly (and NOT just use the popup)
if (chrome.action && chrome.action.onClicked && chrome.action.onClicked.addListener) {
    chrome.action.onClicked.addListener((tab) => {
        console.log("[ChromiumPeek] Action icon clicked on tab:", tab.id);
    });
}
