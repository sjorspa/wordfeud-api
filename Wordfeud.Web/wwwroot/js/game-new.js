// ============================================
// Wordfeud Web - New Game Page JS
// ============================================

function copyGameId() {
    const gameIdEl = document.getElementById('game-id-display');
    if (gameIdEl) {
        copyToClipboard(gameIdEl.textContent).then(() => {
            const copyText = document.getElementById('copy-text');
            if (copyText) {
                copyText.textContent = 'Copied!';
                setTimeout(() => { copyText.textContent = 'Copy'; }, 2000);
            }
        });
    }
}

// On page load, if we have a gameId, start auto-refresh
document.addEventListener('DOMContentLoaded', () => {
    const gameId = '@Model.GameId';
    if (gameId) {
        // Store game ID for later
        setStoredGameId(gameId);

        // Auto-refresh to check if opponent has joined
        startAutoRefresh(`/games/${gameId}`, (data) => {
            if (data && data.status === 'Active') {
                stopAutoRefresh();
                showToast('Opponent has joined! Enter the game.', 'success');
                // Auto-redirect after a delay
                setTimeout(() => {
                    window.location.href = `/game/${gameId}`;
                }, 2000);
            }
        }, 3000);
    }
});
