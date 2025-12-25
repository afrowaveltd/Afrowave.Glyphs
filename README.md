# Afrowave Glyph Editor

> A professional pixel art glyph editor for creating and managing bitmap fonts with multi-size support and advanced drawing tools.

[![.NET](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS-lightgrey)](https://github.com/afrowaveltd/Afrowave.Glyphs)

![Afrowave Glyph Editor Screenshot](docs/screenshot.png)

## ✨ Features

### 🎨 Professional Drawing Tools
- **✏️ Pencil Tool** - Free-hand pixel drawing with mouse drag support
- **📏 Line Tool** - Draw perfect straight lines using Bresenham's algorithm
- **▭ Rectangle Tool** - Create rectangle outlines with live preview
- **○ Circle Tool** - Draw circles with adjustable radius
- **🪣 Fill Tool** - Flood fill for quick area painting
- **↶↷ Undo/Redo** - Up to 50 levels of history

### 📥 Font Import & Export
- **Import TTF/OTF Fonts** - Convert TrueType and OpenType fonts to bitmap glyphs
- **Automatic Range Import** - Import entire Unicode ranges with one click
- **Batch Import** - Import all glyphs that exist in a font (0-65535)
- **Smart Overwrite Control** - Choose to overwrite or skip existing glyphs
- **Live Preview** - See imported glyphs in real-time
- **🔎 Glyph Browser** - Browse and edit existing glyphs with keyboard navigation
- **Unicode Range Wizard** - Create and edit glyphs in specific UTF ranges

### 🎯 Advanced Preview Features
- **UTF Escape Sequences** - Use `\uXXXX`, `\U+XXXX`, or `{U+XXXX}` to display Unicode characters
- **Dynamic Current Glyph** - Use `{current}` to preview the glyph you're editing in real-time
- **Multi-line Preview** - Full support for multi-line text with Enter and Tab
- **Live Preview Updates** - See changes immediately as you draw

### 📐 Multi-Size Support
Supports various glyph sizes from tiny LCD displays to high-resolution pixel art:
- **LCD Displays**: 5×7, 5×8, 6×8, 5×10
- **Classic Computers**: 7×9 (CGA/EGA), 8×8 (C64, ZX Spectrum), 8×14, 8×16 (VGA), 9×16
- **Modern Terminals**: 10×20, 12×16, 16×16, 16×32
- **High Resolution**: 24×24, 32×32, 48×48, 64×64

### 🗂️ Organization & Management
- **Pack System** - Organize glyphs by size (e.g., `8x16`, `16x32`)
- **Style System** - Multiple font styles per pack (e.g., `_base_`, `arabic`, `emojis`)
- **Workspace Management** - Default workspace: `Documents/Afrowave/GlyphEditor`
- **Auto-save Settings** - Remembers your last pack, style, and preferences

### 🖥️ User Interface
- **Live Preview** - Real-time text rendering with your glyphs
- **Dual Theme Support** - Dark and light themes with automatic contrast
- **Keyboard Shortcuts** - Fast workflow with hotkeys (P, L, R, C, F)
- **Visual Tool Indicators** - Highlighted active tool with blue background
- **Hercules-style Green Preview** - High visibility terminal-style rendering
- **Side-by-side Layout** - Editor and preview in parallel for efficient workflow
- **Responsive Design** - Auto-scaling preview based on glyph size
- **Consistent Styling** - All dialogs and windows match the selected theme

## 🚀 Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) or later
- Windows 11, Linux, or macOS

### Installation

1. Clone the repository:
```bash
git clone https://github.com/afrowaveltd/Afrowave.Glyphs.git
cd Afrowave.Glyphs
```

2. Build the solution:
```bash
dotnet build
```

3. Run the Avalonia (GUI) editor:
```bash
dotnet run --project Editor.Avalonia
```

Or run the Consolonia (Terminal UI) editor:
```bash
dotnet run --project Editor.Consolonia
```

### First Steps

1. **Open Workspace** (📁) - Click to select or create a workspace folder
2. **Create Symbols** (📝) - Initialize the workspace structure
3. **Import Font** (📥) - Import a TTF/OTF font to get started quickly
   - Use "📥 Import Range" for specific Unicode ranges (e.g., 32-126 for ASCII)
   - Or use "🌍 Import All" to import all available glyphs
4. **Browse Glyphs** (🔎) - Open glyph browser to see what's available
5. **Edit & Customize** - Select any glyph and start drawing!
6. **Preview** - Use `{current}` in preview text to see your edits in real-time

## ⌨️ Keyboard Shortcuts

### Tools
| Shortcut | Tool |
|----------|------|
| `P` | Pencil (free-hand drawing) |
| `L` | Line (straight lines) |
| `R` | Rectangle (outlines) |
| `C` | Circle (round shapes) |
| `F` | Fill (flood fill) |

### Actions
| Shortcut | Action |
|----------|--------|
| `Ctrl+Z` | Undo (up to 50 steps) |
| `Ctrl+Y` | Redo |
| `Ctrl+S` | Save glyph (when dirty) |
| `Alt+Tab` | Switch between Browser and Editor |
| `Enter` | New line in preview text |
| `Tab` | Insert tab in preview text |

### UI Buttons
| Button | Action |
|--------|--------|
| 📁 | Open Workspace |
| 📝 | Create Symbols structure |
| 📦 | Create new Pack (size) |
| 🎨 | Create new Style |
| 📥 | Import Font (TTF/OTF) |
| 🔎 | Browse Existing Glyphs |
| 🔢 | Unicode Range Wizard |
| 🌙/☀️ | Toggle Dark/Light Theme |
| 🔄 | Reload Workspace |

## 📁 Project Structure

```
Afrowave.Glyphs/
├── Core/                    # Core models and algorithms
├── Storage.Abstractions/    # Storage interface definitions
├── Storage.FileSystem/      # File-based storage implementation
├── Runtime/                 # Text rendering and glyph resolution
├── App/                     # Application logic (Tools project)
├── Editor.Avalonia/         # Desktop GUI (Avalonia)
└── Editor.Consolonia/       # Terminal UI (Consolonia)
```

### Workspace Structure

Default workspace: `Documents/Afrowave/GlyphEditor/`

```
GlyphEditor/
└── Symbols/
    ├── 8x16/               # Pack (size)
    │   ├── _base_/        # Style
    │   │   ├── A.glyph
    │   │   ├── B.glyph
    │   │   └── _missing.glyph
    │   └── emojis/
    ├── 16x16/
    └── 5x8/
```

## 🎨 Usage Examples

### Using UTF Escape Sequences in Preview

Preview text supports special escape sequences for displaying Unicode characters:

#### **1. `{current}` - Preview Currently Edited Glyph**
```
Current glyph: {current}
```
Automatically displays the glyph you're currently editing. Updates in real-time as you switch glyphs!

#### **2. `\uXXXX` - Standard Unicode Escape (4 hex digits)**
```
Greek alphabet: \u03B1 \u03B2 \u03B3 \u03B4
```
Result: `α β γ δ`

#### **3. `\U+XXXX` or `{U+XXXX}` - Flexible UTF Escape**
```
Math symbols: {U+2211} {U+221E} {U+2260}
Tone marks: \U+2E5 \U+2E6 \U+2E7
```
Result: `∑ ∞ ≠` and tone marks

#### **Example: Combined Usage**
```
Hello BOS! 👋

Testing Greek: \u03B1\u03B2
Math: {U+2211} {U+221E}

Currently editing: {current}
```

### Creating a Simple Font

1. **Create a new pack**: Click "📦 New Pack" and enter size (e.g., `8x16`)
2. **Select base style**: Choose `_base_` from the Style dropdown
3. **Import alphabet**: Use "📥 Import Font" → "📥 Import Range" (32-126)
4. **Preview**: Type text in the preview box to see your font!

### Drawing Custom Glyphs

1. **Select a glyph**: Click on a character in the preview or type in the editor
2. **Choose a tool**: Click ✏️ Pencil or press `P`
3. **Draw**: Click and drag to draw pixels
4. **Save**: Click 💾 Save or press `Ctrl+S`

### Browsing and Editing Existing Glyphs

Use the **Glyph Browser** (🔎) to quickly navigate and edit existing glyphs:

1. **Open Browser**: Click 🔎 "Browse Existing Glyphs" button
2. **Navigate**: Use Previous/Next buttons or arrow keys to browse
3. **Edit**: Click "✏️ Edit" to load glyph into main editor
4. **Continue**: Browser stays open for quick workflow
5. **Refresh**: Click 🔄 to reload after saving changes

**Workflow tip**: Open Browser → Select glyph → Edit → Alt+Tab to main window → Draw → Save → Alt+Tab back → Next glyph

### Organizing Fonts by Style

Create different styles for the same size:
- `8x16/_base_/` - Standard ASCII characters
- `8x16/arabic/` - Arabic script
- `8x16/emojis/` - Emoji and symbols
- `8x16/cyrillic/` - Cyrillic alphabet

## 🛠️ Technologies

- **Framework**: .NET 10 / .NET Standard 2.1
- **GUI**: [Avalonia](https://avaloniaui.net/) (cross-platform XAML)
- **TUI**: [Consolonia](https://github.com/jinek/Consolonia) (terminal interface)
- **Architecture**: MVVM pattern with clean separation of concerns

## 📝 File Format

Glyphs are stored in a simple hex-encoded text format (`.glyph`):

```
# Example: 8x16 bitmap for letter 'A'
8x16
0018
0024
0024
0042
0042
007E
0081
0081
0000
0000
0000
0000
0000
0000
0000
0000
```

Each line represents a row of pixels in hexadecimal format.

## 💡 Tips & Tricks

### Efficient Workflow
1. **Quick Glyph Switching**: Use Glyph Browser (🔎) to browse existing glyphs without closing it
2. **Live Preview Testing**: Use `{current}` in preview text to see your edits in real-time
3. **UTF Character Preview**: Test specific Unicode characters with `\uXXXX` or `{U+XXXX}` escapes
4. **Batch Editing**: Keep Browser open, edit multiple glyphs sequentially with Edit → Alt+Tab → Save → Alt+Tab → Next

### Preview Text Examples
```
Standard text with live glyph:
Current: {current}

Greek letters:
\u03B1\u03B2\u03B3 (alpha, beta, gamma)

Math symbols:
{U+2211} {U+221E} {U+2260} (sum, infinity, not equal)

Multi-line with tabs:
Char	Code	Preview
A	0x41	{U+41}
B	0x42	{U+42}
```

### Theme Switching
- **Light Theme**: Better for bright environments, high contrast borders
- **Dark Theme**: Easier on eyes, Hercules terminal green aesthetic
- **Toggle anytime**: Click 🌙/☀️ button (applies to all dialogs instantly)

### Organizing Large Font Projects
```
Symbols/
├── 8x16/
│   ├── _base_/          # ASCII + basic Latin
│   ├── latin-ext/       # Extended Latin characters
│   ├── cyrillic/        # Russian, Ukrainian, etc.
│   ├── arabic/          # Arabic script
│   ├── symbols/         # Mathematical symbols
│   └── emojis/          # Emoji collection
└── 16x32/               # High-res version of the same
    ├── _base_/
    └── ...
```

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Bresenham's line and circle algorithms for perfect pixel-precise drawing
- Avalonia team for the excellent cross-platform UI framework
- Consolonia team for terminal UI support

## 📧 Contact

- **GitHub**: [afrowaveltd](https://github.com/afrowaveltd)
- **Project**: [Afrowave.Glyphs](https://github.com/afrowaveltd/Afrowave.Glyphs)

## 🗺️ Roadmap

- [ ] Export to common bitmap font formats (BDF, PCF, FON)
- [ ] Animation support for animated glyphs
- [ ] Color glyph support (RGBA bitmaps)
- [ ] Web-based online editor
- [ ] Plugin system for custom tools
- [ ] Collaboration features (shared workspaces)

---

Made with ❤️ by Afrowave
