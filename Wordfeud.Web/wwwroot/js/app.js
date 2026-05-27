// ============================================
// Wordfeud Web - Global Application JS
// ============================================

// API base path - proxied through Wordfeud.Web
const API_BASE = '/api';

// ============================================
// Toast Notification System
// ============================================
function showToast(message, type = 'info', duration = 4000) {
    const container = document.getElementById('toast-container');
    if (!container) return;

    const colors = {
        success: 'bg-green-600 border-green-400',
        error: 'bg-red-600 border-red-400',
        warning: 'bg-yellow-600 border-yellow-400',
        info: 'bg-blue-600 border-blue-400'
    };

    const icons = {
        success: '&#10003;',
        error: '&#10007;',
        warning: '&#9888;',
        info: '&#8505;'
    };

    const toast = document.createElement('div');
    toast.className = `toast-enter ${colors[type]} border-l-4 text-white px-4 py-3 rounded-lg shadow-xl flex items-center gap-3`;
    toast.innerHTML = `
        <span class="text-lg">${icons[type]}</span>
        <span class="flex-1 text-sm">${message}</span>
        <button onclick="this.parentElement.classList.remove('toast-enter'); this.parentElement.classList.add('toast-exit'); setTimeout(() => this.parentElement.remove(), 300)" class="text-white/70 hover:text-white">&times;</button>
    `;

    container.appendChild(toast);

    setTimeout(() => {
        toast.classList.remove('toast-enter');
        toast.classList.add('toast-exit');
        setTimeout(() => toast.remove(), 300);
    }, duration);
}

// ============================================
// API Helper Functions
// ============================================
async function apiFetch(endpoint, options = {}) {
    const url = endpoint.startsWith('/') ? `${API_BASE}${endpoint}` : `${API_BASE}/${endpoint}`;

    const defaultOptions = {
        headers: {
            'Content-Type': 'application/json',
            ...options.headers
        }
    };

    const config = { ...defaultOptions, ...options };
    if (options.body && !(options.body instanceof FormData)) {
        config.body = JSON.stringify(options.body);
    }

    try {
        const response = await fetch(url, config);

        if (!response.ok) {
            let errorDetail = `HTTP ${response.status}`;
            try {
                const err = await response.json();
                errorDetail = err.detail || err.message || err.error || errorDetail;
            } catch {
                errorDetail = response.statusText || errorDetail;
            }
            throw new Error(errorDetail);
        }

        // Check if response has content
        const contentType = response.headers.get('content-type');
        if (contentType && contentType.includes('application/json')) {
            return await response.json();
        }
        return null;
    } catch (error) {
        showToast(error.message, 'error');
        throw error;
    }
}

async function apiGet(endpoint) {
    return apiFetch(endpoint, { method: 'GET' });
}

async function apiPost(endpoint, body) {
    return apiFetch(endpoint, { method: 'POST', body });
}

async function apiPut(endpoint, body) {
    return apiFetch(endpoint, { method: 'PUT', body });
}

async function apiDelete(endpoint) {
    return apiFetch(endpoint, { method: 'DELETE' });
}

// ============================================
// Copy to Clipboard
// ============================================
async function copyToClipboard(text) {
    try {
        await navigator.clipboard.writeText(text);
        showToast('Copied to clipboard!', 'success');
        return true;
    } catch {
        // Fallback
        const textarea = document.createElement('textarea');
        textarea.value = text;
        document.body.appendChild(textarea);
        textarea.select();
        document.execCommand('copy');
        document.body.removeChild(textarea);
        showToast('Copied to clipboard!', 'success');
        return true;
    }
}

// ============================================
// localStorage Helpers
// ============================================
function getStoredPlayerId() {
    return localStorage.getItem('wordfeud-player-id');
}

function setStoredPlayerId(id) {
    localStorage.setItem('wordfeud-player-id', id);
}

function getStoredPlayerName() {
    return localStorage.getItem('wordfeud-player-name');
}

function setStoredPlayerName(name) {
    localStorage.setItem('wordfeud-player-name', name);
}

function getStoredGameId() {
    return localStorage.getItem('wordfeud-game-id');
}

function setStoredGameId(id) {
    localStorage.setItem('wordfeud-game-id', id);
}

// ============================================
// Loading State Helpers
// ============================================
function showLoading(buttonId) {
    const btn = document.getElementById(buttonId);
    if (btn) {
        btn.disabled = true;
        btn.dataset.originalText = btn.innerHTML;
        btn.innerHTML = '<div class="spinner inline-block"></div>';
    }
}

function hideLoading(buttonId) {
    const btn = document.getElementById(buttonId);
    if (btn && btn.dataset.originalText) {
        btn.disabled = false;
        btn.innerHTML = btn.dataset.originalText;
    }
}

// ============================================
// Auto-refresh for waiting state
// ============================================
let refreshInterval = null;

function startAutoRefresh(url, callback, interval = 3000) {
    stopAutoRefresh();
    refreshInterval = setInterval(async () => {
        try {
            const data = await apiGet(url);
            callback(data);
        } catch {
            // Silently fail - toast already shown
        }
    }, interval);
}

function stopAutoRefresh() {
    if (refreshInterval) {
        clearInterval(refreshInterval);
        refreshInterval = null;
    }
}

// ============================================
// Initialize
// ============================================
document.addEventListener('DOMContentLoaded', () => {
    // Auto-fill player name if stored
    const nameInput = document.querySelector('input[name="PlayerName"]');
    if (nameInput) {
        const storedName = getStoredPlayerName();
        if (storedName) {
            nameInput.value = storedName;
        }
    }
});
