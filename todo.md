# Wordfeud Frontend - Todo / Missing Features

## ✅ Completed
- [x] Invisible tile bug fix (handleDrop renders visible tile element)
- [x] API endpoint path fixes (`/place`, `/swap`)
- [x] PlaceTilesRequest format fix
- [x] PlayerId query param on all actions
- [x] Drag & drop tile placement
- [x] Tile rack rendering
- [x] Board rendering with bonus squares
- [x] Pass turn functionality
- [x] Swap tiles modal
- [x] Blank tile letter picker
- [x] Game loop auto-refresh
- [x] Score display
- [x] Move history display
- [x] Game over overlay
- [x] Toast notification system
- [x] ProxyController for API forwarding

## ⏳ Missing / To Implement

### Core Features
- [ ] **Undo last move** - No undo functionality exists
- [ ] **Tile scoring preview** - Show potential score before placing
- [ ] **Keyboard shortcuts** - No keyboard support (Ctrl+Z, spacebar, etc.)
- [ ] **Sound effects** - No audio feedback on tile placement
- [ ] **Animations** - Tile placement could be smoother with CSS transitions
- [ ] **Tile bag animation** - No visual for drawing tiles from bag

### Game Flow
- [ ] **Rename player** - Cannot change player name after creation
- [ ] **Leave game** - No way to leave a game without closing browser
- [ ] **Game rematch** - No rematch button after game over
- [ ] **Spectator mode** - Cannot spectate a game without joining
- [ ] **Game timer/countdown** - No turn timer or game clock
- [ ] **Auto-pass on timeout** - No automatic pass when timer expires

### UI/UX
- [ ] **Responsive improvements** - Board may overflow on very small screens
- [ ] **Dark/light theme toggle** - Currently only dark theme
- [ ] **Board zoom controls** - No zoom in/out for the board
- [ ] **Tile highlight on hover** - Subtle hover effect on rack tiles
- [x] **Suggested moves** - Not needed (out of scope)
- [ ] **Board coordinate labels** - No row/column labels visible
- [ ] **Last move highlight** - Don't highlight which tiles were just placed
- [ ] **Loading skeleton screens** - Currently shows spinner, could use skeleton

### Multiplayer / Networking
- [ ] **WebSocket real-time updates** - Currently polling every 3s, could use WebSockets
- [ ] **Chat between players** - No in-game chat
- [ ] **Emotes/reactions** - No way to send quick reactions
- [ ] **Disconnect detection** - No handling for lost connections
- [ ] **Reconnection logic** - No auto-reconnect on network issues

### Board & Rules
- [ ] **Invalid placement validation** - Frontend doesn't validate word existence
- [ ] **Word dictionary check** - No dictionary integration
- [ ] **Diagonal placement** - Only horizontal/vertical supported
- [ ] **First move must pass through center** - Not validated on frontend
- [ ] **Adjacent tile rule** - Not validated that tiles connect to existing ones
- [ ] **All tiles must be in a line** - Partially validated but could be stricter

### Settings & Preferences
- [ ] **Tile size preference** - Cannot customize tile/board size
- [ ] **Colorblind mode** - No alternative color scheme for bonus squares
- [ ] **Sound volume control** - No volume settings (even though no sound yet)
- [ ] **Language/locale** - UI is hardcoded in English
- [ ] **Remember last game** - Partially implemented via localStorage

### Data & Storage
- [ ] **Game export** - Cannot export game state/history
- [ ] **Game import** - Cannot load a game from exported data
- [ ] **Local game save** - Cannot save draft games locally
- [ ] **Statistics tracking** - No win/loss/draw stats
- [ ] **Personal best scores** - No tracking of personal records

### Admin / Management
- [ ] **Game list** - No view of all active games
- [ ] **Delete game** - No way to delete a game
- [ ] **Archive games** - No game archive feature
- [ ] **Player rankings** - No leaderboard or rankings
- [ ] **Game analytics** - No stats on move times, scores, etc.

### Performance
- [ ] **Virtual scrolling for move history** - Could be slow with many moves
- [ ] **Debounced API calls** - Game loop polls every 3s regardless of activity
- [ ] **Service Worker** - No offline support or caching
- [ ] **Lazy load images** - No images currently, but tiles could be image-based

### Accessibility
- [ ] **ARIA labels** - Missing ARIA attributes for screen readers
- [ ] **Keyboard navigation** - Cannot navigate board with keyboard
- [ ] **Focus management** - No focus traps in modals
- [ ] **Screen reader announcements** - No live region updates
- [ ] **Reduced motion support** - No respect for prefers-reduced-motion

### Deployment
- [ ] **Docker Compose setup** - No docker-compose.yml for local dev
- [ ] **Kubernetes manifests** - No K8s deployment files
- [ ] **CI/CD pipeline** - No GitHub Actions or other CI config
- [ ] **Environment config** - Config could be more flexible
- [ ] **Health check endpoint** - API has it but frontend doesn't check it

### Testing
- [ ] **Unit tests for JS** - No frontend tests
- [ ] **Integration tests** - No E2E tests
- [ ] **API contract tests** - No tests for API ↔ frontend compatibility
- [ ] **Visual regression tests** - No screenshot comparison
- [ ] **Load testing** - No testing for concurrent games

### Documentation
- [ ] **README for frontend** - No frontend-specific README
- [ ] **API documentation** - Swagger exists but no usage examples
- [ ] **Architecture diagram** - No docs on how frontend ↔ API communicate
- [ ] **Contributing guide** - No contribution guidelines
- [ ] **Changelog** - No history of changes documented

### Security
- [ ] **CORS configuration** - Could be more restrictive
- [ ] **Rate limiting** - No rate limiting on frontend requests
- [ ] **Input sanitization** - Player names not sanitized on frontend
- [ ] **XSS protection** - InnerHTML usage could be safer
- [ ] **CSRF tokens** - Not implemented (may not be needed with proxy)
