// popup.js

let connection = null;

document.addEventListener("DOMContentLoaded", () => {
    const button = document.getElementById("hello-btn");
    const status = document.getElementById("status");

    button.addEventListener("click", async () => {
        // Already connected? Don't reconnect.
        if (connection && connection.state === "Connected") {
            status.textContent = "Already connected to Peek hub.";
            return;
        }

        status.textContent = "Connecting to Peek hub...";

        try {
            // Create the connection if we don't have one yet
            if (!connection) {
                connection = new signalR.HubConnectionBuilder()
                    .withUrl("http://localhost:9956/hub/v4/g4/peek")
                    .withAutomaticReconnect()
                    .build();

                // Optional: basic logging
                connection.onreconnecting((error) => {
                    console.warn("[ChromiumPeek] Reconnecting to hub...", error);
                    status.textContent = "Reconnecting to Peek hub...";
                });

                connection.onreconnected((connectionId) => {
                    console.log("[ChromiumPeek] Reconnected to hub:", connectionId);
                    status.textContent = "Reconnected to Peek hub.";
                });

                connection.onclose((error) => {
                    console.warn("[ChromiumPeek] Connection closed:", error);
                    status.textContent = "Connection closed.";
                });

                // Later you'll add handlers like:
                // connection.on("SomeServerEvent", data => { ... });
            }

            // Start the connection
            await connection.start();
            console.log("[ChromiumPeek] Connected to hub.");
            status.textContent = "Connected to Peek hub.";

        } catch (err) {
            console.error("[ChromiumPeek] Failed to connect to hub:", err);
            status.textContent = "Failed to connect – see console.";
        }
    });
});
