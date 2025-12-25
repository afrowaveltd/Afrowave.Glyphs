# Afrowave Glyph Editor - Současný stav verzí

## 🎯 **Doporučení použití:**

### **Avalonia (GUI) - DOPORUČENO pro produkci** ✅
- ✅ Plná funkcionalita
- ✅ Native OS dialogy
- ✅ Grafické preview
- ✅ Pixel-perfect glyph editor
- ✅ Font import s file pickerem
- ✅ Unicode range wizard
- ✅ Browse glyphs window

**Použij pro:** Vývoj, import fontů, pixel art editing

---

### **Consolonia (TUI) - Pro quick viewing** ⚠️
- ✅ Pack/Style selection
- ✅ Custom W/H size
- ✅ Glyph selection (text input)
- ✅ ASCII art preview (real-time)
- ✅ Save/Revert/Clear/Clone
- ❌ Glyph Editor (crash - multiple windows)
- ❌ Font Import (crash - multiple windows)
- ❌ Unicode Range (crash - multiple windows)

**Použij pro:** Quick preview, terminal workflow, remote SSH

---

## 📋 **Pracovní workflow:**

### **Optimální použití:**

```
1. Import fontu → Avalonia
2. Hromadná editace → Avalonia
3. Quick preview → Consolonia (SSH)
4. Final export → Avalonia
```

### **Pro remote work (SSH):**

```
1. Consolonia pro preview existujících glyphů
2. Clone/Save základní operace
3. Pro složitější editaci → local Avalonia přes X11 forwarding
```

---

## ✅ **Co funguje v obou verzích:**

| Feature | Avalonia | Consolonia |
|---------|----------|------------|
| Pack selector | ✅ | ✅ |
| Style selector | ✅ | ✅ |
| Custom W/H | ✅ | ✅ |
| Glyph selection | ✅ | ✅ |
| Save/Revert/Clear | ✅ | ✅ |
| Clone glyph | ✅ | ✅ |
| New style | ✅ | ✅ |
| **Glyph editor** | ✅ | ❌ |
| **Font import** | ✅ | ❌ |
| **Unicode range** | ✅ | ❌ |
| **Preview** | Full render | ASCII art |

---

## 🔧 **Budoucí vylepšení (Consolonia):**

Pokud budeš chtít plnou paritu, je potřeba:

1. **Přepsat dialogy na inline views** (Single Window Architecture)
2. **Glyph Editor** → inline pixel grid (WASD controls)
3. **Font Import** → inline form (text input for path)
4. **Unicode Range** → inline form (start/end inputs)

**Odhadovaný čas:** 4-8 hodin práce

---

## 💡 **Pro současnost - WORKAROUND:**

### **Glyph editing v Consolonia:**

```csharp
// Místo GlyphEditorWindow:
// 1. Zobraz inline pixel grid
// 2. WASD + Space pro drawing
// 3. Enter/Esc pro accept/cancel
```

### **Font import v Consolonia:**

```csharp
// Místo FontImportWizardWindow:
// 1. Text input pro cestu k fontu
// 2. Range inputs (start/end)
// 3. Button "Import All"
```

---

## 🚀 **Závěr:**

### **Pro production:**
→ **Použij Avalonia** ✅

### **Pro quick viewing/SSH:**
→ **Použij Consolonia** (s omezeními) ⚠️

### **Pro plnou paritu:**
→ Implementuj Single Window Architecture v Consolonia (budoucnost)

---

**Obě verze jsou funkční pro své use-cases!**

**Avalonia = full-featured editor**
**Consolonia = quick viewer/terminal tool**

✅ Funkční stav pro MVP! 🎉
