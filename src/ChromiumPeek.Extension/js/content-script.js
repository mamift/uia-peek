// content-script.js

console.log("[ChromiumPeek] Hello from content script on:", window.location.href);

// As a visible smoke test, mark the page once:
if (!window.__chromiumPeekHelloInjected) {
    window.__chromiumPeekHelloInjected = true;

    const banner = document.createElement("div");
    banner.textContent = "ChromiumPeek: Hello World!";
    banner.style.position = "fixed";
    banner.style.zIndex = 999999;
    banner.style.bottom = "10px";
    banner.style.right = "10px";
    banner.style.padding = "6px 10px";
    banner.style.borderRadius = "4px";
    banner.style.fontFamily = "system-ui, sans-serif";
    banner.style.fontSize = "12px";
    banner.style.background = "rgba(0, 0, 0, 0.8)";
    banner.style.color = "#fff";
    banner.style.pointerEvents = "none";

    document.body.appendChild(banner);

    // Auto-remove after a few seconds
    setTimeout(() => banner.remove(), 3000);
}
