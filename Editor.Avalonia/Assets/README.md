# Afrowave Glyph Editor - Icon Creation Guide

## 📏 Required Files

You need to create **2 files**:

1. **icon.ico** - Multi-resolution Windows icon (256+48+32+16)
2. **icon.png** - Single 256×256 PNG for Avalonia

---

## 🎨 How to Create in GIMP

### Step 1: Import SVG Template
1. Open GIMP
2. **File → Open** → Select `icon-template.svg`
3. Import at **256×256 pixels**

### Step 2: Export PNG Sizes

Export these 4 PNG files:

```
File → Export As → icon-256.png (256×256)
Image → Scale Image → 48×48 → Export As → icon-48.png
Image → Scale Image → 32×32 → Export As → icon-32.png  
Image → Scale Image → 16×16 → Export As → icon-16.png
```

**Tips for scaling:**
- Use **Cubic interpolation** for best quality
- Check "Flatten image" when exporting PNG

### Step 3: Create Multi-Resolution ICO

GIMP cannot create multi-resolution ICO directly. Use one of these methods:

#### Option A: Online Converter (EASIEST) ✅
1. Go to **https://convertio.co/png-ico/**
2. Upload all 4 PNG files
3. Select "Multi-size ICO" option
4. Download `icon.ico`

#### Option B: ImageMagick (if installed)
```sh
magick convert icon-256.png icon-48.png icon-32.png icon-16.png icon.ico
```

#### Option C: IcoFX (Windows App)
- Download: https://icofx.ro/
- Import 4 PNGs → Save as ICO

---

## 📂 Final File Structure

```
Editor.Avalonia\Assets\
├── icon.ico              ← Windows app icon (256+48+32+16)
├── icon.png              ← Rename icon-256.png to this
├── icon-template.svg     ← SVG source (keep for future edits)
└── README.md            ← This file
```

---

## 🛠️ Enable Icons in Project

After creating `icon.ico` and `icon.png`, **uncomment these lines** in `Editor.Avalonia.csproj`:

```xml
<!-- UNCOMMENT THIS: -->
<ApplicationIcon>Assets\icon.ico</ApplicationIcon>

<!-- AND THIS: -->
<ItemGroup>
  <AvaloniaResource Include="Assets\icon.png" />
</ItemGroup>
```

---

## 🎨 Icon Design Details

**Colors:**
- Background: `#1a1a1a` (dark gray)
- Grid: `#2a2a2a` (subtle darker gray)
- Main "A": `#00ff00` (Hercules green)
- Accents: `#ff8800`, `#ffaa00` (African orange/yellow)
- Grid indicator: `#00aa00` (darker green)

**Elements:**
- Pixel art letter "A" (representing Afrowave)
- Subtle grid background (representing glyph editor)
- Two orange circles (African pattern accent)
- 3×3 pixel grid indicator (bottom right)
- Rounded corners (modern look)

---

## 🔄 Quick Summary

1. ✅ Open `icon-template.svg` in GIMP at 256×256
2. ✅ Export 4 PNG files (256, 48, 32, 16)
3. ✅ Combine PNGs into `icon.ico` (use online tool)
4. ✅ Rename `icon-256.png` → `icon.png`
5. ✅ Uncomment lines in `Editor.Avalonia.csproj`
6. ✅ Rebuild project

Done! 🎉
