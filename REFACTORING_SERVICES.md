# Refaktoring - Sdílené služby

## Provedené změny:

### ✅ **1. Přesunutí sdílených služeb do Tools projektu**

Následující třídy byly přesunuty z `Editor.Avalonia.Services` a `Editor.Consolonia.Services` do **`Tools.Services`**:

- **`EditorWorkspaceService`** → `App/Services/EditorWorkspaceService.cs`
- **`TerminalGlyphCache`** → `App/Services/TerminalGlyphCache.cs`

### ✅ **2. Opravené namespace v projektechá**

**Editor.Avalonia:**
- `MainWindow.axaml.cs` - změněno `using Editor.Avalonia.Services;` → `using Tools.Services;`
- `App.axaml.cs` - změněno `using Editor.Avalonia.Services;` → `using Tools.Services;`
- `Controls/TerminalPreviewControl.axaml.cs` - změněno namespace
- `Controls/GlyphPreviewControl.axaml.cs` - změněno namespace

**Editor.Consolonia:**
- `MainWindow.axaml.cs` - změněno `using Editor.Avalonia.Services;` → `using Tools.Services;`
- `App.axaml.cs` - změněno `using Editor.Avalonia.Services;` → `using Tools.Services;`

### ✅ **3. Opravený FilePickerWindow**

**`Editor.Consolonia/FilePickerWindow.axaml.cs`:**
- Změněno `await ShowDialog(owner);` zpět na `Show();`
- **Důvod**: Consolonia neumožňuje vnořené dialogy, ale povoluje ne-modální okna pomocí `Show()`
- `TaskCompletionSource` správně čeká na `Closed` event

### 📁 **Struktura projektu:**

```
App/ (Tools.csproj)
  └── Services/
      ├── EditorWorkspaceService.cs  ✅ SHARED
      └── TerminalGlyphCache.cs      ✅ SHARED

Editor.Avalonia/
  └── Services/
      ├── EditorWorkspaceService.cs  ❌ DEPRECATED (lze smazat)
      └── TerminalGlymphCache.cs     ❌ DEPRECATED (lze smazat)

Editor.Consolonia/
  └── Services/
      ├── EditorWorkspaceService.cs  ❌ DEPRECATED (lze smazat)
      └── TerminalGlyphCache.cs      ❌ DEPRECATED (lze smazat)
```

### 🧹 **Další kroky (volitelné):**

1. **Odstranit staré duplicitní soubory:**
   ```powershell
   Remove-Item "Editor.Avalonia/Services/EditorWorkspaceService.cs"
   Remove-Item "Editor.Avalonia/Services/TerminalGlymphCache.cs"
   Remove-Item "Editor.Consolonia/Services/EditorWorkspaceService.cs"
   Remove-Item "Editor.Consolonia/Services/TerminalGlyphCache.cs"
   ```

2. **Odstranit prázdné Services složky** (pokud již neobsahují nic jiného)

### ✅ **Výhody refaktoringu:**

1. ✅ **Žádná duplikace kódu** - služby jsou nyní na jednom místě
2. ✅ **Konzistence** - obě aplikace používají stejnou logiku
3. ✅ **Snadnější údržba** - změny se projeví v obou projektech automaticky
4. ✅ **Menší velikost** - eliminace duplicitních souborů

### 🎯 **Výsledek:**

- ✅ **Editor.Avalonia** - sestaví se bez chyb
- ✅ **Editor.Consolonia** - sestaví se bez chyb
- ✅ **FilePickerWindow** - funguje správně (ne-modální okno v Consolonia)
- ✅ **Workspace** - sdílená logika pro oba projekty

---

## Chyba "Creating multiple Window objects simultaneously"

### **Problém:**
Consolonia nepodporuje více modálních dialogů současně.

### **Řešení:**
`FilePickerWindow.PickAsync()` nyní používá:
```csharp
Show();  // Ne-modální okno
return _tcs.Task;  // Čeká na Closed event
```

Místo:
```csharp
await ShowDialog(owner);  // ❌ Způsobovalo deadlock
```

### **Jak to funguje:**
1. `Show()` otevře okno jako **ne-modální**
2. User vybere soubor/složku a klikne "Select"
3. `OnSelect()` nastaví `_tcs.TrySetResult(path)`
4. `Close()` uzavře okno
5. `Closed` event triggeruje fallback `_tcs.TrySetResult(null)`
6. Volající kód obdrží výsledek přes `await _tcs.Task`

✅ **Funguje perfektně!**
