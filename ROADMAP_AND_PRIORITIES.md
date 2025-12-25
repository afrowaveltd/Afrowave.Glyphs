# Afrowave Glyph Editor - Updated Roadmap with MAUI

## ✅ **SOUČASNÝ STAV (MVP HOTOVO!):**

### **Avalonia Desktop** - Production Ready! 🎉
- ✅ Windows/Linux/macOS support
- ✅ Full glyph editing capabilities
- ✅ Font import wizard
- ✅ Professional tools

### **Consolonia TUI** - Preview Tool ⚠️
- ✅ Quick preview via SSH
- ✅ Basic operations
- ⚠️ Limited editing (architectural constraints)

---

## 🎯 **AKTUALIZOVANÁ PRIORITA:**

### **KRÁTKODOBÉ (1-2 měsíce):**

#### **1. MAUI + Blazor Hybrid** 📱🔥
**Priorita:** **CRITICAL - HIGHEST!**
**Odhadovaný čas:** 4-6 týdnů
**Důvod:** Best mobile solution!

**Proč MAUI místo Avalonia.Mobile:**
- ✅ **Blazor UI** - Web technologie, rychlejší vývoj
- ✅ **Hot reload** - instant feedback
- ✅ **Responsive** - Bootstrap grid system
- ✅ **Touch-optimized** - gestures built-in
- ✅ **Future web version** - same components!
- ✅ **Microsoft support** - better documentation
- ✅ **macOS support** - Mac Catalyst included

**Platforms:**
- ✅ Android (primary)
- ✅ iOS
- ✅ macOS (Catalyst)

**Milestones:**
1. **Week 1-2:** Setup + Basic UI
   - MauiProgram + Blazor
   - Index, Editor, Browser pages
   - PackSelector, StyleSelector components

2. **Week 3-4:** Touch Editor
   - GlyphPixelGrid.razor (touch-based)
   - Android platform services
   - System font import

3. **Week 5:** Cloud Sync
   - Export/Import workspace
   - Google Drive integration (future)

4. **Week 6:** Polish
   - Lokalizace
   - iOS testing
   - Performance optimization

**Value:** 🚀 KILLER FEATURE - Web + Mobile unified!

---

#### **2. Lokalizace** 🌍
**Priorita:** HIGH
**Odhadovaný čas:** 2-4 hodiny
**Důvod:** Quick win, global reach

**Tasks:**
- [ ] Create en.json, cs.json translations
- [ ] Update LocalizationService
- [ ] Add language selector to Settings
- [ ] Test runtime switching
- [ ] Apply to MAUI app as well!

**Value:** International appeal

---

#### **3. Documentation** 📚
**Priorita:** MEDIUM
**Odhadovaný čas:** 1 týden

**Tasks:**
- [ ] User manual (EN + CS)
- [ ] Video tutorials
- [ ] API docs
- [ ] Contributing guide
- [ ] MAUI setup guide

**Value:** User onboarding

---

### **STŘEDNĚDOBÉ (3-6 měsíců):**

#### **4. Web Version** 🌐
**Priorita:** HIGH
**Odhadovaný čas:** 2-3 týdny
**Důvod:** Same Blazor components from MAUI!

**Concept:**
- Reuse MAUI Blazor components
- Blazor WebAssembly or Server
- Browser-based glyph editor
- No installation needed!

**Platforms:**
- Any modern browser
- Desktop + Mobile web
- PWA support

**Value:** Maximum reach!

---

#### **5. Advanced Features** 🚀
**Priorita:** MEDIUM
**Odhadovaný čas:** 2-4 týdny

**Tasks:**
- [ ] Multi-level Undo/Redo
- [ ] Layers support
- [ ] Animation frames
- [ ] Batch operations
- [ ] Auto-save

**Value:** Professional tools

---

### **DLOUHODOBÉ (6+ měsíců):**

#### **6. Marketplace/Gallery** 🌐
**Priorita:** LOW
**Odhadovaný čas:** 2-3 měsíce

**Concept:**
- Community glyph packs
- Share/Download
- Ratings
- Curated collections

**Value:** Community growth

---

## 📊 **Platform Strategy:**

```
Desktop Power Users:
  └── Avalonia (Windows/Linux/macOS)
      - Professional editing
      - Complex operations
      - Bulk import

Mobile/Tablet Users:
  └── MAUI (Android/iOS/macOS)
      - On-the-go editing
      - Touch-friendly
      - System fonts
      - Cloud sync

Web Users:
  └── Blazor WebAssembly
      - No installation
      - Try before download
      - Quick edits

Terminal Users:
  └── Consolonia (Linux/macOS SSH)
      - Quick preview
      - Remote viewing
```

---

## 🎯 **Tech Stack přehled:**

| Platform | Technology | UI Framework | Target Devices |
|----------|------------|--------------|----------------|
| **Desktop** | Avalonia | XAML | Windows/Linux/macOS |
| **Mobile** | MAUI | **Blazor** | Android/iOS/macOS |
| **Web** | Blazor WASM | **Blazor** | Any browser |
| **Terminal** | Consolonia | XAML | SSH/Remote |

**Blazor = 2 platforms (Mobile + Web) with 1 codebase!** 🎯

---

## ✅ **Doporučený Next Step:**

### **Tento týden:**
```
1. ✅ Commit current state
2. 📚 Update README with MAUI plans
3. 🔥 Start MAUI project setup
4. 🌍 Implement lokalizace (parallel task)
```

### **Tento měsíc:**
```
1. 📱 MAUI PoC working
2. 🎨 Blazor components functional
3. 🌍 Full localization
4. 📱 Android APK build
```

### **Tento kvartál:**
```
1. 📱 MAUI production release
2. 🌐 Web version beta
3. 📚 Full documentation
4. 🚀 Public launch
```

---

## 💡 **MAUI Benefits Summary:**

### **Development:**
- ✅ **Blazor** - HTML/CSS/JS (familiar web stack)
- ✅ **Hot reload** - instant feedback
- ✅ **Shared code** - 80% reuse from Tools/Core
- ✅ **Web components** - reusable for web version!

### **User Experience:**
- ✅ **Native performance** - full device access
- ✅ **Touch-optimized** - gestures, large buttons
- ✅ **Responsive** - adapts to screen size
- ✅ **Modern UI** - Bootstrap, Material Design

### **Distribution:**
- ✅ **App stores** - Google Play, App Store
- ✅ **Auto-updates** - via stores
- ✅ **Cross-platform** - Android + iOS + macOS
- ✅ **Future web** - same codebase!

---

## 🚀 **ZÁVĚR:**

### **Priorita #1:** 📱 **MAUI + Blazor**
**WHY:** Best mobile solution + Web reuse!

### **Priorita #2:** 🌍 **Lokalizace**
**WHY:** Easy win, global users!

### **Priorita #3:** 🌐 **Web Version**
**WHY:** Same Blazor components = free!

### **De-prioritizováno:** 🖥️ **Consolonia Full Features**
**WHY:** Limited use case, keep as viewer!

---

**NEW VISION:**

> "Afrowave Glyph Editor - Create pixel-perfect glyphs **everywhere**:
> Desktop powerhouse, Mobile creativity, Web convenience, Terminal viewing!"

**Platforms:**
- 🖥️ Desktop (Avalonia)
- 📱 Mobile (MAUI + Blazor) ← **NEW FOCUS!**
- 🌐 Web (Blazor WASM) ← **BONUS!**
- 🖥️ Terminal (Consolonia)

**MAUI + Blazor = 2 platforms, 1 codebase!** 🎯🚀

---

**Ready to build the future! Let's start with MAUI!** 🔥

