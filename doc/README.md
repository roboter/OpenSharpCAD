# OpenSharpCAD Interactive 3D Documentation

Interactive WebGL Three.js documentation for OpenSharpCAD featuring side-by-side 3D models and syntax-highlighted, read-only C# code snippets.

## Features

- **Side-by-Side Layout**: Interactive 3D WebGL viewport on the left, syntax-highlighted C# code on the right (identical layout to OpenSharpCAD's native F1 reference window).
- **Interactive Three.js Viewports**:
  - Drag with left mouse button to rotate and inspect models from any angle.
  - Right-click drag to pan.
  - Scroll wheel to zoom.
  - Viewport HUD controls: `🏠 Home` (center/fit view), `🌐 Wire` (toggle wireframe mode), `🔄 Spin` (auto-tumble rotation), and zoom `+` / `-` buttons.
- **Dracula Dark Theme**: Styled to match the OpenSharpCAD IDE visual palette.
- **1-Click Copy**: Copy any example's C# code to clipboard with instant feedback.
- **Instant Search & Category Filtering**: Filter examples by category (`3D Shapes`, `2D Primitives`, `2D => 3D`, `CSG Booleans`, `Complex Assemblies`) or search by name / code keywords.
- **High Performance**: Employs `IntersectionObserver` to animate and render only visible viewports on demand.

## Pages

- **[Interactive 3D Examples](index.html)** (`doc/index.html`): Interactive Three.js 3D viewport examples with side-by-side Dracula syntax-highlighted C# code.
- **[API Reference](api.html)** (`doc/api.html`): Full programmatic API reference covering 3D Solids, 2D Primitives, Transformations, CSG Boolean operations, and Script Globals with live search filtering, constructor signatures, properties, methods, and copyable snippets.

## Viewing Locally

Open `doc/index.html` or `doc/api.html` directly in any modern web browser or serve via any static file server:

```bash
# Python
python3 -m http.server 8080 --directory doc

# Node.js
npx serve doc
```

Then navigate to:
- `http://localhost:8080/index.html` — Interactive 3D Examples
- `http://localhost:8080/api.html` — Programmatic API Reference
