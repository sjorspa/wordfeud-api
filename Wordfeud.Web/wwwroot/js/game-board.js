// ============================================
// Wordfeud Web - Game Board JS
// Complete drag-and-drop tile game engine
// ============================================

// ============================================
// Game State
// ============================================
const GameState = {
    gameId: null,
    currentPlayerId: null,
    selectedTileId: null,
    placedTiles: new Map(), // key: "row,col" -> value: tileId
    draggedTileId: null,
    blankTileTargetId: null,
    swapSelectedTiles: new Set(),
    refreshInterval: null,
    lastGameState: null,
    lastMoveTiles: new Set() // Set of "row,col" strings for highlighting
};

// ============================================
// Initialize Game Board
// ============================================
document.addEventListener('DOMContentLoaded', () => {
    // Get game ID from URL
    const pathParts = window.location.pathname.split('/');
    if (pathParts.length >= 3) {
        GameState.gameId = pathParts[2];
    }

    // Initialize player ID from hidden input
    const hiddenEl = document.getElementById('current-player-id');
    if (hiddenEl && hiddenEl.value) {
        GameState.currentPlayerId = hiddenEl.value;
        localStorage.setItem('wordfeud-player-id', hiddenEl.value);
    }

    if (GameState.gameId) {
        setStoredGameId(GameState.gameId);
        loadGameState();
        startGameLoop();
        initKeyboardShortcuts();
    }
});

// ============================================
// Keyboard Shortcuts
// ============================================
function initKeyboardShortcuts() {
    document.addEventListener('keydown', (e) => {
        // Ctrl+Z - Recall tiles
        if ((e.ctrlKey || e.metaKey) && e.key === 'z') {
            e.preventDefault();
            recallTiles();
            return;
        }

        // Space or Enter - Place word
        if ((e.key === ' ' || e.key === 'Enter') && GameState.placedTiles.size > 0) {
            e.preventDefault();
            placeWord();
            return;
        }

        // Escape - Clear placed tiles
        if (e.key === 'Escape') {
            if (GameState.placedTiles.size > 0) {
                recallTiles();
            }
            // Close any open modals
            const modals = document.querySelectorAll('.modal:not(.hidden)');
            modals.forEach(m => m.classList.add('hidden'));
            return;
        }

        // P - Pass turn
        if (e.key === 'p' && !e.ctrlKey && !e.metaKey) {
            e.preventDefault();
            passTurn();
            return;
        }

        // S - Open swap modal
        if (e.key === 's' && !e.ctrlKey && !e.metaKey) {
            e.preventDefault();
            openSwapModal();
            return;
        }
    });
}

// ============================================
// Load Game State from API
// ============================================
async function loadGameState() {
    if (!GameState.gameId) return;

    try {
        const data = await apiGet(`/games/${GameState.gameId}`);
        if (!data) return;

        GameState.lastGameState = JSON.parse(JSON.stringify(data));
        
        // Initialize currentPlayerId from hidden input or localStorage
        if (!GameState.currentPlayerId) {
            const hiddenEl = document.getElementById('current-player-id');
            if (hiddenEl && hiddenEl.value) {
                GameState.currentPlayerId = hiddenEl.value;
            } else {
                GameState.currentPlayerId = localStorage.getItem('wordfeud-player-id') || 'local-player';
            }
        }

        // Update UI based on game state
        updateScorePanel(data.players);
        updateGameInfo(data);
        updateMoveHistory(data.moves || []);
        extractLastMoveTiles(data.moves || []);
        updateBoard(data.board || []);
        updateTileRack(data.players.find(p => p.id === GameState.currentPlayerId)?.hand || []);
        updateTurnIndicator(data.currentPlayerId);

        // Check if game is over
        if (data.status === 'Finished') {
            stopGameLoop();
            showGameOver(data);
            return;
        }

        // Check if it's opponent's turn - start faster refresh
        if (data.currentPlayerId !== GameState.currentPlayerId) {
            showToast(`Waiting for ${data.players.find(p => p.id === data.currentPlayerId)?.name || 'opponent'}...`, 'info', 3000);
        }
    } catch (error) {
        console.error('Failed to load game state:', error);
    }
}

// ============================================
// Game Loop - Auto-refresh
// ============================================
function startGameLoop() {
    stopGameLoop();
    GameState.refreshInterval = setInterval(async () => {
        if (!GameState.gameId) return;
        try {
            const data = await apiGet(`/games/${GameState.gameId}`);
            if (!data) return;

            // Only update if state changed
            const currentStateStr = JSON.stringify(data);
            if (currentStateStr !== JSON.stringify(GameState.lastGameState)) {
                GameState.lastGameState = JSON.parse(currentStateStr);

                // Check if game is over
                if (data.status === 'Finished') {
                    stopGameLoop();
                    showGameOver(data);
                    return;
                }

                // Update UI
                updateScorePanel(data.players);
                updateGameInfo(data);
                updateMoveHistory(data.moves || []);
                updateBoard(data.board || []);
                updateTileRack(data.players.find(p => p.id === GameState.currentPlayerId)?.hand || []);
                updateTurnIndicator(data.currentPlayerId);

                // If it's our turn and we have no placed tiles, notify
                if (data.currentPlayerId === GameState.currentPlayerId && GameState.placedTiles.size === 0) {
                    showToast('It\'s your turn! Place a word.', 'success', 3000);
                }
            }
        } catch (error) {
            console.error('Game loop refresh failed:', error);
        }
    }, 3000);
}

function stopGameLoop() {
    if (GameState.refreshInterval) {
        clearInterval(GameState.refreshInterval);
        GameState.refreshInterval = null;
    }
}

// ============================================
// Update UI Components
// ============================================
function updateScorePanel(players) {
    const scorePanel = document.querySelector('.bg-gray-800 .space-y-2');
    if (!scorePanel) return;

    const currentYou = players.find(p => p.id === GameState.currentPlayerId);
    players.forEach(player => {
        const el = document.querySelector(`[data-player-id="${player.id}"] .score-value`);
        if (el) {
            el.textContent = player.score;
        }
    });
}

function updateGameInfo(data) {
    // Update bag count, move number, etc.
    const bagEl = document.querySelector('.bg-gray-800 .space-y-2 .font-mono.text-white');
    if (bagEl && data.bagCount !== undefined) {
        const allMonoEls = document.querySelectorAll('.bg-gray-800 .space-y-2 .font-mono.text-white');
        if (allMonoEls.length >= 2) {
            allMonoEls[1].textContent = data.bagCount;
        }
    }
}

function updateMoveHistory(moves) {
    const historyContainer = document.querySelector('.max-h-64.overflow-y-auto');
    if (!historyContainer) return;

    if (moves.length === 0) {
        historyContainer.innerHTML = '<p class="text-gray-500 text-sm">No moves yet</p>';
        return;
    }

    historyContainer.innerHTML = moves.map(move => `
        <div class="text-xs p-2 bg-gray-900 rounded">
            <span class="font-semibold text-tile-gold">${move.playerName}</span>
            <span class="text-gray-400 ml-1">placed "${move.word}"</span>
            <span class="text-green-400 ml-1">+${move.points} pts</span>
        </div>
    `).join('');

    // Scroll to bottom
    historyContainer.scrollTop = historyContainer.scrollHeight;
}

function extractLastMoveTiles(moves) {
    GameState.lastMoveTiles = new Set();
    if (!moves || moves.length === 0) return;

    // Find the last place move (highest move number with tiles)
    const placeMoves = moves.filter(m => m.actionType === 'place' && m.tiles && m.tiles.length > 0);
    if (placeMoves.length === 0) return;

    const lastMove = placeMoves[placeMoves.length - 1];
    for (const tile of lastMove.tiles) {
        GameState.lastMoveTiles.add(`${tile.row},${tile.column}`);
    }
}

function updateTurnIndicator(currentPlayerId) {
    const indicator = document.querySelector('.mb-4 .rounded-full');
    if (!indicator) return;

    const isYourTurn = currentPlayerId === GameState.currentPlayerId;
    indicator.className = `mb-4 px-6 py-2 rounded-full ${isYourTurn ? 'bg-green-600' : 'bg-yellow-600'}`;
    indicator.innerHTML = `<span class="font-semibold">${isYourTurn ? 'Your Turn' : "Opponent's Turn"}</span>`;
}

function updateTileRack(hand) {
    const rack = document.getElementById('tile-rack');
    if (!rack) return;

    rack.innerHTML = hand.map(tile => {
        const blankClass = tile.isBlank ? 'blank-tile' : '';
        return `
            <div class="tile ${blankClass}"
                 data-tile-id="${tile.id}"
                 data-points="${tile.points}"
                 draggable="true"
                 ondragstart="handleDragStart(event, '${tile.id}')"
                 onclick="handleTileClick('${tile.id}')">
                ${tile.isBlank ? '?' : tile.letter}
                <span class="points">${tile.points}</span>
            </div>
        `;
    }).join('');
}

function updateBoard(board) {
    // Update placed tiles on the board
    // Clear existing placed tiles
    document.querySelectorAll('.placed-tile').forEach(el => el.remove());

    if (!board || board.length === 0) return;

    for (let row = 0; row < board.length; row++) {
        for (let col = 0; col < board[row].length; col++) {
            const tile = board[row][col];
            if (tile) {
                const cell = getBoardCell(row, col);
                if (cell) {
                    const tileEl = document.createElement('div');
                    const isLastMove = GameState.lastMoveTiles.has(`${row},${col}`);
                    tileEl.className = `placed-tile${isLastMove ? ' last-move' : ''}`;
                    tileEl.innerHTML = `
                        ${tile.isBlank ? '?' : tile.letter}
                        <span class="points">${tile.points}</span>
                        <button class="remove-btn" onclick="removePlacedTile(${row}, ${col})">&times;</button>
                    `;
                    cell.appendChild(tileEl);
                }
            }
        }
    }

    updatePlacedTilesPreview();
}

function getBoardCell(row, col) {
    return document.querySelector(`.board-cell[data-row="${row}"][data-col="${col}"]`);
}

// ============================================
// Drag and Drop Handlers
// ============================================
function handleDragStart(event, tileId) {
    GameState.draggedTileId = tileId;
    event.dataTransfer.effectAllowed = 'move';
    event.dataTransfer.setData('text/plain', tileId);

    // Visual feedback
    const tileEl = document.querySelector(`[data-tile-id="${tileId}"]`);
    if (tileEl) {
        tileEl.style.opacity = '0.5';
        setTimeout(() => { tileEl.style.opacity = '1'; }, 100);
    }
}

// Track hover position for scoring preview
let hoveredCell = null;
let scoringPreviewEl = null;

function getBoardCell(row, col) {
    return document.querySelector(`.board-cell[data-row="${row}"][data-col="${col}"]`);
}

function getScoringPreview() {
    if (!scoringPreviewEl) {
        scoringPreviewEl = document.createElement('div');
        scoringPreviewEl.className = 'scoring-preview';
        scoringPreviewEl.style.display = 'none';
        document.body.appendChild(scoringPreviewEl);
    }
    return scoringPreviewEl;
}

function showScoringPreview(row, col, score) {
    const preview = getScoringPreview();
    const cell = getBoardCell(row, col);
    if (!cell) return;

    const rect = cell.getBoundingClientRect();
    preview.textContent = `Score: ${score}`;
    preview.style.display = 'block';
    preview.style.top = `${rect.top - 45}px`;
    preview.style.left = `${rect.left + rect.width / 2}px`;
    preview.classList.add('visible');
}

function hideScoringPreview() {
    const preview = getScoringPreview();
    if (preview) {
        preview.classList.remove('visible');
    }
}

// Bonus square multiplier lookup
const BONUS_MULTIPLIERS = {
    'TW': { type: 'word', multiplier: 3 },
    'DW': { type: 'word', multiplier: 2 },
    'TL': { type: 'letter', multiplier: 3 },
    'DL': { type: 'letter', multiplier: 2 },
    'STAR': { type: 'word', multiplier: 1 }
};

function getCellBonus(row, col) {
    // Double Word
    const dw = [[1,1],[2,2],[3,3],[4,4],[1,13],[2,12],[3,11],[4,10],[10,4],[11,3],[12,2],[13,1],[10,10],[11,11],[12,12],[13,13]];
    if (dw.some(p => p[0] === row && p[1] === col)) return 'DW';

    // Triple Word
    const tw = [[0,0],[0,7],[0,14],[7,0],[7,14],[14,0],[14,7],[14,14]];
    if (tw.some(p => p[0] === row && p[1] === col)) return 'TW';

    // Triple Letter
    const tl = [[1,5],[1,9],[5,1],[5,5],[5,9],[5,13],[9,1],[9,5],[9,9],[9,13],[13,5],[13,9]];
    if (tl.some(p => p[0] === row && p[1] === col)) return 'TL';

    // Double Letter
    const dl = [[0,3],[0,11],[2,6],[2,8],[3,0],[3,7],[3,14],[6,2],[6,6],[6,8],[6,12],[7,3],[7,11],[8,2],[8,6],[8,8],[8,12],[11,0],[11,7],[11,14],[12,6],[12,8],[14,3],[14,11]];
    if (dl.some(p => p[0] === row && p[1] === col)) return 'DL';

    // Star (center)
    if (row === 7 && col === 7) return 'STAR';

    return '';
}

// Calculate estimated score for a single tile on a cell
function calculateTileScore(tile, row, col) {
    if (!tile) return 0;

    let baseScore = tile.points || 0;
    const bonus = getCellBonus(row, col);
    const bonusInfo = BONUS_MULTIPLIERS[bonus];

    if (!bonusInfo) return baseScore;

    if (bonusInfo.type === 'letter') {
        return baseScore * bonusInfo.multiplier;
    } else {
        // Word bonus - only show base score since word bonuses depend on full word
        return baseScore;
    }
}

function handleDragEnter(row, col) {
    if (!GameState.draggedTileId) return;

    const hand = getCurrentHand();
    const tile = hand.find(t => t.id === GameState.draggedTileId);
    if (!tile) return;

    const score = calculateTileScore(tile, row, col);
    showScoringPreview(row, col, score);
    hoveredCell = { row, col };
}

function handleDragLeave() {
    hideScoringPreview();
    hoveredCell = null;
}

function handleDrop(event, row, col) {
    event.preventDefault();
    event.stopPropagation();

    const cell = event.currentTarget;
    cell.classList.remove('drop-hover');

    // Check if cell is already occupied by a placed tile
    const existingPlaced = cell.querySelector('.placed-tile');
    if (existingPlaced) {
        showToast('Cell is already occupied', 'warning');
        return;
    }

    const tileId = event.dataTransfer.getData('text/plain') || GameState.draggedTileId;
    if (!tileId) return;

    // Check if tile is already placed elsewhere
    for (const [key, val] of GameState.placedTiles.entries()) {
        if (val === tileId) {
            // Remove from old position
            const [oldRow, oldCol] = key.split(',').map(Number);
            const oldCell = getBoardCell(oldRow, oldCol);
            if (oldCell) {
                const oldTileEl = oldCell.querySelector('.placed-tile');
                if (oldTileEl) oldTileEl.remove();
            }
            GameState.placedTiles.delete(key);
            break;
        }
    }

    // Place tile at new position
    GameState.placedTiles.set(`${row},${col}`, tileId);
    cell.classList.add('drop-hover');
    setTimeout(() => cell.classList.remove('drop-hover'), 200);

    // Get tile data from the hand to render it visually
    const hand = getCurrentHand();
    const tileData = hand.find(t => t.id === tileId);

    // Render the tile visually on the board cell
    if (cell && tileData) {
        // If blank tile, show picker modal instead of rendering immediately
        if (tileData.isBlank) {
            GameState.blankTileTargetId = tileId;
            showBlankLetterPicker(tileId);
        } else {
            const tileEl = document.createElement('div');
            tileEl.className = 'placed-tile';
            tileEl.innerHTML = `
                ${tileData.isBlank ? '?' : tileData.letter}
                <span class="points">${tileData.points}</span>
                <button class="remove-btn" onclick="removePlacedTile(${row}, ${col})">&times;</button>
            `;
            cell.appendChild(tileEl);
        }
    }

    updatePlacedTilesPreview();
    GameState.draggedTileId = null;
}

// ============================================
// Click to Select/Place Tiles
// ============================================
function handleTileClick(tileId) {
    // If tile is already placed on board, remove it
    for (const [key, val] of GameState.placedTiles.entries()) {
        if (val === tileId) {
            const [row, col] = key.split(',').map(Number);
            removePlacedTile(row, col);
            return;
        }
    }

    // Select tile from rack
    if (GameState.selectedTileId === tileId) {
        // Deselect
        GameState.selectedTileId = null;
        document.querySelector(`[data-tile-id="${tileId}"]`)?.classList.remove('selected');
    } else {
        // Deselect previous
        document.querySelector(`.tile.selected`)?.classList.remove('selected');
        GameState.selectedTileId = tileId;
        document.querySelector(`[data-tile-id="${tileId}"]`)?.classList.add('selected');
        showToast('Click on a board cell to place the tile', 'info', 2000);
    }
}

function removePlacedTile(row, col) {
    const key = `${row},${col}`;
    const tileId = GameState.placedTiles.get(key);
    if (!tileId) return;

    GameState.placedTiles.delete(key);

    const cell = getBoardCell(row, col);
    if (cell) {
        const tileEl = cell.querySelector('.placed-tile');
        if (tileEl) {
            tileEl.remove();
        }
    }

    updatePlacedTilesPreview();
}

// ============================================
// Placed Tiles Preview
// ============================================
function updatePlacedTilesPreview() {
    const preview = document.getElementById('placed-tiles-preview');
    if (!preview) return;

    if (GameState.placedTiles.size === 0) {
        preview.innerHTML = '<span class="text-gray-500 text-sm">Drag tiles from your rack onto the board</span>';
        return;
    }

    // Build word from placed tiles
    const tiles = Array.from(GameState.placedTiles.entries())
        .map(([key, tileId]) => {
            const [row, col] = key.split(',').map(Number);
            const hand = getCurrentHand();
            const tile = hand.find(t => t.id === tileId);
            return { key, row, col, tile: tile || { letter: '?', points: 0 } };
        })
        .sort((a, b) => a.row !== b.row ? a.row - b.row : a.col - b.col);

    // Determine orientation
    const isHorizontal = tiles.every(t => t.row === tiles[0].row);
    const isVertical = tiles.every(t => t.col === tiles[0].col);

    if (!isHorizontal && !isVertical) {
        preview.innerHTML = '<span class="text-red-400 text-sm">Tiles must be placed in a straight line</span>';
        return;
    }

    // Build word string
    let word = '';
    if (isHorizontal) {
        const cols = [...new Set(tiles.map(t => t.col))].sort((a, b) => a - b);
        word = cols.map(col => {
            const tile = tiles.find(t => t.col === col);
            return tile ? (tile.tile.isBlank ? '?' : tile.tile.letter) : '';
        }).join('');
    } else {
        const rows = [...new Set(tiles.map(t => t.row))].sort((a, b) => a - b);
        word = rows.map(row => {
            const tile = tiles.find(t => t.row === row);
            return tile ? (tile.tile.isBlank ? '?' : tile.tile.letter) : '';
        }).join('');
    }

    preview.innerHTML = `
        <span class="text-tile-gold font-bold text-lg mr-2">${word}</span>
        <span class="text-gray-400 text-sm">(${GameState.placedTiles.size} tiles placed)</span>
        <span class="ml-auto text-sm text-gray-500">Click tiles on board to remove</span>
    `;
}

// ============================================
// Place Word
// ============================================
async function placeWord() {
    if (GameState.placedTiles.size === 0) {
        showToast('Place at least one tile on the board', 'warning');
        return;
    }

    // Validate straight line
    const tiles = Array.from(GameState.placedTiles.entries());
    const isHorizontal = tiles.every(t => t[0].split(',')[0] === tiles[0][0].split(',')[0]);
    const isVertical = tiles.every(t => t[0].split(',')[1] === tiles[0][0].split(',')[1]);

    if (!isHorizontal && !isVertical) {
        showToast('Tiles must be placed in a straight line', 'error');
        return;
    }

    // Check connectivity to center or existing tiles
    const firstRow = parseInt(tiles[0][0].split(',')[0]);
    const firstCol = parseInt(tiles[0][0].split(',')[1]);

    // Get the current hand to find tile data (letter, isBlank, points)
    const hand = getCurrentHand();

    // Build the request body matching the API DTO: {letter, isBlank, tileId, row, column}
    const body = {
        tiles: tiles.map(([key, tileId]) => {
            const [row, col] = key.split(',').map(Number);
            const tileData = hand.find(t => t.id === tileId);
            return {
                letter: tileData?.letter || '',
                isBlank: tileData?.isBlank || false,
                tileId: tileId,
                row: row,
                column: col
            };
        })
    };

    try {
        showLoading('place-btn');
        const result = await apiPost(`/games/${GameState.gameId}/place?playerId=${GameState.currentPlayerId}`, body);

        if (result) {
            showToast(`Word placed! +${result.points || 0} points`, 'success');
            GameState.placedTiles.clear();
            updatePlacedTilesPreview();
            await loadGameState();
        }
    } catch (error) {
        showToast(error.message || 'Failed to place word', 'error');
    } finally {
        hideLoading('place-btn');
    }
}

// ============================================
// Recall Tiles
// ============================================
async function recallTiles() {
    if (GameState.placedTiles.size === 0) {
        showToast('No tiles to recall', 'warning');
        return;
    }

    if (!confirm('Recall all placed tiles?')) return;

    GameState.placedTiles.clear();

    // Remove from board UI
    document.querySelectorAll('.placed-tile').forEach(el => el.remove());
    updatePlacedTilesPreview();
    showToast('Tiles recalled', 'info');
}

// ============================================
// Pass Turn
// ============================================
async function passTurn() {
    if (GameState.placedTiles.size > 0) {
        showToast('Place or recall your tiles first', 'warning');
        return;
    }

    try {
        const result = await apiPost(`/games/${GameState.gameId}/pass?playerId=${GameState.currentPlayerId}`, {});
        showToast('Turn passed', 'info');
        await loadGameState();
    } catch (error) {
        showToast(error.message || 'Failed to pass turn', 'error');
    }
}

// ============================================
// Swap Tiles
// ============================================
function showSwapModal() {
    const modal = document.getElementById('swap-modal');
    const list = document.getElementById('swap-tile-list');

    if (!modal || !list) return;

    const hand = getCurrentHand();
    GameState.swapSelectedTiles.clear();

    list.innerHTML = hand.map(tile => `
        <label class="flex items-center gap-3 p-2 bg-gray-900 rounded-lg cursor-pointer hover:bg-gray-700">
            <input type="checkbox" value="${tile.id}" onchange="toggleSwapTile('${tile.id}', this.checked)" class="w-4 h-4 rounded">
            <div class="tile text-sm" style="width: 2rem; height: 2rem;">
                ${tile.isBlank ? '?' : tile.letter}
                <span class="points">${tile.points}</span>
            </div>
            <span class="text-gray-300 text-sm">${tile.isBlank ? 'Blank' : tile.letter}</span>
        </label>
    `).join('');

    modal.classList.remove('hidden');
}

function closeSwapModal() {
    const modal = document.getElementById('swap-modal');
    if (modal) modal.classList.add('hidden');
}

function toggleSwapTile(tileId, checked) {
    if (checked) {
        GameState.swapSelectedTiles.add(tileId);
    } else {
        GameState.swapSelectedTiles.delete(tileId);
    }
}

async function confirmSwap() {
    if (GameState.swapSelectedTiles.size === 0) {
        showToast('Select at least one tile to swap', 'warning');
        return;
    }

    if (GameState.swapSelectedTiles.size > 3) {
        showToast('You can swap up to 3 tiles', 'warning');
        return;
    }

    try {
        const result = await apiPost(`/games/${GameState.gameId}/swap?playerId=${GameState.currentPlayerId}`, {
            tileIds: Array.from(GameState.swapSelectedTiles)
        });

        if (result) {
            showToast('Tiles swapped successfully', 'success');
            closeSwapModal();
            await loadGameState();
        }
    } catch (error) {
        showToast(error.message || 'Failed to swap tiles', 'error');
    }
}

// ============================================
// Blank Tile Letter Picker
// ============================================
function showBlankLetterPicker(targetTileId) {
    GameState.blankTileTargetId = targetTileId;
    const modal = document.getElementById('blank-modal');
    if (modal) modal.classList.remove('hidden');
}

function closeBlankModal() {
    const modal = document.getElementById('blank-modal');
    if (modal) modal.classList.add('hidden');
    GameState.blankTileTargetId = null;
}

async function selectBlankLetter(letter) {
    if (!GameState.blankTileTargetId) {
        closeBlankModal();
        return;
    }

    // Find the cell where this blank tile was dropped
    for (const [key, tileId] of GameState.placedTiles.entries()) {
        if (tileId === GameState.blankTileTargetId) {
            const [row, col] = key.split(',').map(Number);
            const cell = getBoardCell(row, col);
            if (cell) {
                // Remove the old placeholder tile and render with selected letter
                const oldTile = cell.querySelector('.placed-tile');
                if (oldTile) oldTile.remove();

                const tileEl = document.createElement('div');
                tileEl.className = 'placed-tile';
                tileEl.innerHTML = `
                    ${letter}
                    <span class="points">0</span>
                    <button class="remove-btn" onclick="removePlacedTile(${row}, ${col})">&times;</button>
                `;
                cell.appendChild(tileEl);
            }
            break;
        }
    }

    updatePlacedTilesPreview();
    closeBlankModal();
    showToast(`Blank tile set to "${letter}"`, 'success');
}

// ============================================
// Game Over
// ============================================
function showGameOver(data) {
    const winner = data.players.find(p => p.isWinner);
    const winnerName = winner ? winner.name : 'No winner';

    const overlay = document.createElement('div');
    overlay.id = 'game-over-overlay';
    overlay.className = 'fixed inset-0 modal-overlay z-50 flex items-center justify-center p-4';
    overlay.innerHTML = `
        <div class="bg-gray-800 rounded-2xl shadow-2xl p-8 max-w-lg w-full text-center">
            <h2 class="text-4xl font-bold text-tile-gold mb-2">&#127942; Game Over!</h2>
            <p class="text-2xl text-white mb-6">${winnerName} wins!</p>

            <div class="bg-gray-900 rounded-xl p-4 mb-6 space-y-3">
                ${data.players.map(p => `
                    <div class="flex justify-between items-center ${p.isWinner ? 'text-green-400' : 'text-white'}">
                        <span class="font-semibold">${p.name} ${p.isWinner ? '&#128525;' : ''}</span>
                        <span class="text-xl font-bold">${p.score}</span>
                    </div>
                `).join('')}
            </div>

            <div class="flex gap-3">
                <a href="/" class="flex-1 py-3 bg-gray-700 hover:bg-gray-600 rounded-xl text-white font-semibold transition-all">
                    &#8962; Home
                </a>
                <a href="/game/new" class="flex-1 py-3 bg-green-600 hover:bg-green-500 rounded-xl text-white font-semibold transition-all">
                    &#10133; Play Again
                </a>
            </div>
        </div>
    `;

    document.body.appendChild(overlay);
}

// ============================================
// Helper Functions
// ============================================
function getCurrentPlayerId() {
    // Get from localStorage (set by server-side session after join)
    const playerId = localStorage.getItem('wordfeud-player-id');
    if (playerId) {
        GameState.currentPlayerId = playerId;
        return playerId;
    }
    
    // Fallback: get from hidden input on page
    const hiddenEl = document.getElementById('current-player-id');
    if (hiddenEl && hiddenEl.value) {
        GameState.currentPlayerId = hiddenEl.value;
        return hiddenEl.value;
    }
    
    // Last fallback to first player
    return GameState.currentPlayerId || 'local-player';
}

function getCurrentHand() {
    if (!GameState.lastGameState || !GameState.lastGameState.players) return [];
    const playerId = GameState.currentPlayerId || localStorage.getItem('wordfeud-player-id');
    const player = GameState.lastGameState.players.find(p => p.id === playerId);
    return player ? player.hand : [];
}

function getStoredPlayerId() {
    return localStorage.getItem('wordfeud-player-id') || 'player-' + Date.now();
}

function setStoredPlayerId(id) {
    localStorage.setItem('wordfeud-player-id', id);
}

// ============================================
// Board cell drag events
// ============================================
document.addEventListener('dragover', (e) => {
    if (e.target.classList.contains('board-cell')) {
        e.preventDefault();
        e.target.classList.add('drop-hover');
    }
});

document.addEventListener('dragleave', (e) => {
    if (e.target.classList.contains('board-cell')) {
        e.target.classList.remove('drop-hover');
    }
});
