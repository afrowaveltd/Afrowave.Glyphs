# Consolonia verze - rozdíly oproti Avalonia

## Omezení Consolonia frameworku:

### 1. **Nemožnost vnořených dialogů**
- ❌ **Nelze** otevřít dialog z dialogu
- ✅ **Řešení**: Použití text inputu místo `FilePickerWindow` uvnitř `FontImportWizardWindow`

### 2. **File picker v Import Font dialogu**
- **Avalonia**: Grafický file picker s browsováním
- **Consolonia**: Text input s možností zadat cestu manuálně
- **Default cesta**: Automaticky vyplněná cesta k system fonts

### 3. **Workspace picker**
- **Funguje**: `FilePickerWindow` se otevírá z hlavního okna (ne z dialogu)
- **Použití**: `OpenWorkspaceAsync()` správně používá `ShowDialog()`

## Jak použít Font Import v Consolonia:

1. Spusťte aplikaci (Consolonia verzi)
2. Klikněte na **"Import"** v horní liště
3. V dialogu **"Enter font file path:"**:
   - Zadejte cestu k `.ttf` nebo `.otf` fontu
   - Nebo upravte předvyplněnou cestu (výchozí: system fonts)
   - Příklad Windows: `C:\Windows\Fonts\arial.ttf`
   - Příklad Linux: `/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf`
4. Klikněte **OK**
5. Postupujte průvodcem importu

## Tipy:

### Rychlé cesty k fontům:
**Windows:**
```
C:\Windows\Fonts\arial.ttf
C:\Windows\Fonts\consola.ttf
C:\Windows\Fonts\cour.ttf
```

**Linux:**
```
/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf
/usr/share/fonts/truetype/liberation/LiberationMono-Regular.ttf
```

**macOS:**
```
/System/Library/Fonts/Monaco.ttf
/System/Library/Fonts/Courier.ttc
```

### Workspace selection:
- **Funguje normálně** - `FilePickerWindow` pro výběr složky
- Browsování adresářů pomocí šipek a Enter
- Select button pro výběr aktuální složky

## Architektura:

```
Main Window (Consolonia)
  ├─ Open Workspace → FilePickerWindow (OK - ShowDialog)
  ├─ Import Font → FontImportWizardWindow (OK - ShowDialog)
  │    └─ Pick Font File → TEXT INPUT (FIX - místo vnořeného FilePickerWindow)
  └─ Unicode Range → UnicodeRangeWizardWindow (OK - ShowDialog)
```

## Budoucí vylepšení (volitelné):

1. **Tab completion** pro cesty
2. **Historie nedávno použitých fontů**
3. **Preset seznam populárních fontů**
4. **Browse button** - ale jen pokud zavřeme wizard před otevřením pickeru
