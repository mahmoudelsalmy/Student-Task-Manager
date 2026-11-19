# TaskMate frontend

Plain HTML/CSS/JavaScript served by the ASP.NET Core app (no build step, no framework).

| File | Purpose |
|---|---|
| `tokens.css` | Design tokens: type scale, spacing, radii, shadows and **light + dark colour themes**, plus a system font stack that uses Inter when available. |
| `styles.css` | Components and layout. Uses tokens only – never hardcode a colour here. |
| `theme-init.js` | Applies the saved/OS theme before first paint (external file because the CSP forbids inline scripts). |
| `app.js` | Application logic. Views are declared once in the `VIEWS`/`PANELS` registry; `activateView()` is the only code that shows/hides panels. |

## Rules
- Add a colour by adding a token to **both** themes in `tokens.css`, then check contrast (text pairs must be ≥ 4.5:1).
- No inline `<script>` or `style=""` attributes: the CSP is `script-src 'self'; style-src 'self'`.
- Every control must call a real API or be removed. Do not add nav items for pages that have no backend yet.
- Destructive actions use `confirmAction()`, not `window.confirm`.
- Theme is stored in `localStorage` key `taskmate.theme` (`light` | `dark`); with no saved value the OS preference is used.
