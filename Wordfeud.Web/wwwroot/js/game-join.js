// ============================================
// Wordfeud Web - Join Game Page JS
// ============================================

document.addEventListener('DOMContentLoaded', () => {
    // Auto-fill player name from localStorage
    const storedName = getStoredPlayerName();
    const nameInput = document.querySelector('input[name="PlayerName"]');
    if (nameInput && storedName) {
        nameInput.value = storedName;
    }

    // Auto-fill game ID from localStorage if available
    const gameId = getStoredGameId();
    const gameIdInput = document.querySelector('input[name="GameId"]');
    if (gameIdInput && gameId) {
        gameIdInput.value = gameId;
    }
});
