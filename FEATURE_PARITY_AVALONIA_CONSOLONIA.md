# Srovnání funkcionality: Avalonia vs Consolonia

## ✅ **Společné funkce (fungují v obou verzích):**

### **1. Pack Management:**
- [x] Pack selector ComboBox
- [x] Load packs from workspace
- [x] Display pack ID (e.g., "8x16")
- [x] Switch between packs
- [x] Auto-reload on workspace change

### **2. Style Management:**
- [x] Style selector ComboBox
- [x] Load styles from current pack
- [x] Display style name (e.g., "_base_")
- [x] Create new style (inline dialog)
- [x] Switch between styles

### **3. Custom Size:**
- [x] Custom W/H input boxes
- [x] Auto-update on change
- [x] Create custom pack if doesn't exist

### **4. Glyph Selection:**
- [x] Glyph ID input (U+0041, A, or name)
- [x] Set/Go button
- [x] Display current glyph

### **5. Glyph Operations:**
- [x] Save glyph
- [x] Revert glyph
- [x] Clear glyph
- [x] Clone glyph
- [ ] **Edit glyph** ← PROBLÉM V CONSOLONIA!

### **6. Preview:**
- [x] Text input
- [x] Real-time preview
- [ ] Avalonia: Full terminal render
- [x] Consolonia: ASCII art (dynamic size)

### **7. Workspace:**
- [x] Avalonia: Native folder picker
- [ ] Consolonia: **NEFUNKČNÍ!** (crash nebo nereaguje)

### **8. Import:**
- [x] Avalonia: Full import wizard with font picker
- [ ] Consolonia: **NEFUNKČNÍ!** (crash na multiple windows)

### **9. Unicode Range:**
- [x] Avalonia: Full range wizard
- [ ] Consolonia: **NEFUNKČNÍ!** (crash)

---

## ❌ **Chybějící funkce v Consolonia:**

1. **Glyph Editor** - crash při otevření
2. **Font Import** - crash při vytvoření window
3. **Unicode Range** - crash při vytvoření window
4. **Workspace picker** - nefunkční nebo jen reload

---

## ✅ **Návrh řešení - Zjednodušené dialogy pro Consolonia:**

### **1. Glyph Editor → Inline pixel grid:**
```
┌─────────────────────────────────┐
│ Editing: U+0041 (A)             │
├─────────────────────────────────┤
│ ████████                        │
│ ██    ██                        │
│ ██    ██                        │
│ ████████                        │
│ ██    ██                        │
│                                 │
│ [W][A][S][D] + [Space] to draw │
│ [Enter] Accept  [Esc] Cancel    │
└─────────────────────────────────┘
```

### **2. Font Import → Text-based wizard:**
```
┌─────────────────────────────────┐
│ Font Import Wizard              │
├─────────────────────────────────┤
│ Font path: [________________]  │
│ Pack: [8x16▼]  Style: [_base_] │
│ Range: [32] to [126]            │
│                                 │
│ [Import] [Cancel]               │
└─────────────────────────────────┘
```

### **3. Unicode Range → Simple list:**
```
┌─────────────────────────────────┐
│ Unicode Range                   │
├─────────────────────────────────┤
│ Start: [32]  End: [126]         │
│ Pack: [8x16]  Style: [_base_]   │
│                                 │
│ [Generate All] [Cancel]         │
└─────────────────────────────────┘
```

---

## 🎯 **Priorita oprav:**

1. **HIGH:** Glyph Editor - bez toho nelze editovat
2. **HIGH:** Font Import - bez toho nelze importovat fonty
3. **MEDIUM:** Unicode Range - užitečné ale lze ručně
4. **LOW:** Workspace picker - lze změnit v settings

---

## 💡 **Doporučení:**

Pro Consolonia použít **zjednodušené inline dialogy** místo separate Windows:

1. Glyph Editor → Replace content with pixel grid
2. Font Import → Replace content with form
3. Unicode Range → Replace content with simple inputs

**Všechny dialogy UVNITŘ MainWindow.Content** - žádné new Window()!

