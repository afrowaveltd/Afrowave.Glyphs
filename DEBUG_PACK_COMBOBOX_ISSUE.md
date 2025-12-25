# Debugging Pack ComboBox Issue

## 🐛 Problém:

Screenshot ukazuje:
- **Pack: "base_"** místo "8x16"
- **Style: "_base_"** (správně)
- **Size: 8x16** (správně)

## 🔍 Analýza:

### **Co by mělo být:**
```
Workspace struktura:
Documents/Afrowave/GlyphEditor/
  └── Symbols/
      └── 8x16/           ← Pack ID (název složky)
          └── _base_/     ← Style ID (název složky)
              └── *.glyph files
```

### **Co se zobrazuje:**
```
Pack ComboBox: "base_"  ← ŠPATNĚ!
```

## 🎯 Možné příčiny:

### **1. Workspace není správně inicializovaný**
```csharp
// App.axaml.cs řádek 57-62
if(string.IsNullOrWhiteSpace(workspaceRoot))
{
   workspaceRoot = EditorWorkspaceService.GetDefaultWorkspaceRoot();
   await workspaceService.AddWorkspaceRootAsync(workspaceRoot);
   await workspaceService.EnsureSymbolsStructureAsync(workspaceRoot);  ← Vytvoří 8x16/_base_
}
```

**Debug steps:**
1. Zkontroluj Output window při spuštění
2. Hledej řádky: `[RELOAD] Adding pack: ...`
3. Pokud tam není "8x16", workspace není správně vytvořený

---

### **2. Workspace root ukazuje na špatnou složku**

**Možný scénář:**
```
workspace = "D:\Projects\CustomFolder"
symbolsRoot = "D:\Projects\CustomFolder\Symbols"  ← SPRÁVNĚ

ALE:
Existující složka: "D:\Projects\CustomFolder\_base_"  ← ŠPATNĚ!
Provider čte tuto složku jako Pack ID
```

**Fix:**
- Zkontroluj fyzickou strukturu workspace složky
- Smaž špatné složky
- Restart aplikaci

---

### **3. FileSystemOptions.SymbolsRootPath je špatně**

```csharp
// FileSystemFontPackProvider.cs řádek 29
foreach(var dir in Directory.EnumerateDirectories(_options.SymbolsRootPath))
{
   var packId = Path.GetFileName(dir);  ← Co se vrátí?
}
```

**Debug:**
```csharp
System.Diagnostics.Debug.WriteLine($"SymbolsRootPath: {_options.SymbolsRootPath}");
foreach(var dir in Directory.EnumerateDirectories(_options.SymbolsRootPath))
{
   System.Diagnostics.Debug.WriteLine($"Found directory: {dir}");
   System.Diagnostics.Debug.WriteLine($"PackId: {Path.GetFileName(dir)}");
}
```

---

## ✅ Opravy v tomto commitu:

### **1. Workspace button - dočasné řešení**
```csharp
private async Task OpenWorkspaceAsync()
{
   // Use current workspace - no dialog to avoid crashes
   var root = _workspace.CurrentWorkspaceRoot;
   await ReinitializeForWorkspaceAsync(root);
   Title = $"Consolonia - {root} (reload)";
}
```

**Proč:**
- Window dialogy způsobují crash v Consolonia
- Dočasně jen reload current workspace
- TODO: Implementovat bez Window class

---

### **2. Layout fix - ComboBox width**
```xml
<!-- PŘED: -->
<ComboBox Grid.Column="1" />  ← Rozlézá se

<!-- PO: -->
<ComboBox Grid.Column="1" MinWidth="10" MaxWidth="15" />  ← Fixní šířka
```

**Výsledek:**
- ✅ Pack ComboBox nepřesahuje
- ✅ Style ComboBox kompaktní
- ✅ W/H inputy menší (Width="5")
- ✅ Helper text "[auto]" místo dlouhého textu

---

### **3. Debug output přidán**
```csharp
System.Diagnostics.Debug.WriteLine($"[APP INIT] PackIds.Count = {vm.PackIds.Count}");
foreach(var p in vm.PackIds)
   System.Diagnostics.Debug.WriteLine($"[APP INIT] PackId: {p}");
```

**Jak použít:**
1. Spusť Consolonia v Debug modu (F5)
2. Otevři Output window (View → Output)
3. Vyber "Debug" z dropdown
4. Hledej řádky s `[APP INIT]` a `[RELOAD]`

---

## 🔧 Návod na řešení:

### **Step 1: Zkontroluj Output window**
```
[RELOAD] ListPacksAsync returned X packs
[RELOAD] Adding pack: ...
[APP INIT] PackIds.Count = X
[APP INIT] PackId: ...
```

**Pokud tam je "base_" místo "8x16":**
→ Workspace obsahuje špatnou složku!

---

### **Step 2: Zkontroluj fyzickou strukturu**

**Otevři průzkumníka:**
```
%USERPROFILE%\Documents\Afrowave\GlyphEditor\Symbols\
```

**Správná struktura:**
```
Symbols/
  └── 8x16/
      └── _base_/
          └── _missing.glyph
```

**Špatná struktura (BUG):**
```
Symbols/
  └── _base_/    ← ŠPATNĚ! Tohle není Pack ID!
```

**Fix:**
1. Smaž špatnou složku `_base_`
2. Restart aplikaci
3. Aplikace vytvoří správnou strukturu `8x16/_base_`

---

### **Step 3: Manuální vytvoření struktury**

Pokud automatická inicializace selže:

```powershell
$workspace = "$env:USERPROFILE\Documents\Afrowave\GlyphEditor"
$symbols = "$workspace\Symbols\8x16\_base_"
New-Item -Path $symbols -ItemType Directory -Force
```

**Restart aplikaci** → Pack ComboBox by měl zobrazit "8x16"

---

## 📊 Expected vs Actual:

| Item | Expected | Actual (Screenshot) | Status |
|------|----------|-------------------|---------|
| Pack | 8x16 | base_ | ❌ BUG |
| Style | _base_ | _base_ | ✅ OK |
| Size | 8x16 | 8x16 | ✅ OK |
| W/H | 8, 16 | 8, 16 | ✅ OK |
| Layout | Compact | Rozlezlý | ⚠️ FIXED |

---

## ✅ Next Steps:

1. **Spusť debug** a zkontroluj Output
2. **Zkontroluj workspace složku** fyzicky
3. **Smaž špatné složky** pokud existují
4. **Restart** aplikaci
5. **Report zpět** co ukázal Output window

---

**Očekávám, že Output window ukáže:**
```
[RELOAD] Adding pack: base_
```

**Místo správného:**
```
[RELOAD] Adding pack: 8x16
```

To potvrdí, že workspace obsahuje špatnou složku! 🔍
