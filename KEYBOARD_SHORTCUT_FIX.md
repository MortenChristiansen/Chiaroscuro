# Keyboard Shortcut Fix for '@' Character Issue

## Problem
The '@' character could not be typed in the browser UI because keyboard shortcuts were using `HasFlag()` to check modifier keys, which allowed unintended modifier combinations to trigger shortcuts.

When typing '@' (typically Ctrl+Alt+2 or AltGr+2 on many keyboards), the Ctrl-2 workspace shortcut was being triggered because `HasFlag(ModifierKeys.Control)` returned true even when Alt was also pressed.

## Solution
Replaced all `HasFlag()` checks with exact equality (`==`) checks to ensure only the intended modifier combinations trigger shortcuts.

### Before (Problematic)
```csharp
if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
```
This would match:
- Ctrl only ✓ (intended)
- Ctrl+Alt ✗ (unintended - causes '@' character conflict)
- Ctrl+Shift ✗ (might be unintended depending on context)

### After (Fixed)
```csharp
if (Keyboard.Modifiers == ModifierKeys.Control)
```
This matches only:
- Ctrl only ✓ (intended)

For Control+Shift combinations:
```csharp
if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
```

## Files Changed
1. `src/BrowserHost/Features/Workspaces/WorkspacesFeature.cs` - Ctrl+1-9 shortcuts
2. `src/BrowserHost/Features/ActionDialog/ActionDialogFeature.cs` - Ctrl+T shortcut
3. `src/BrowserHost/Features/Tabs/TabsFeature.cs` - Ctrl+X, Ctrl+B shortcuts
4. `src/BrowserHost/Features/Folders/FoldersFeature.cs` - Ctrl+G shortcut
5. `src/BrowserHost/Features/PinnedTabs/PinnedTabsFeature.cs` - Ctrl+P shortcut
6. `src/BrowserHost/Features/Zoom/ZoomFeature.cs` - Ctrl+MouseWheel, Ctrl+Backspace shortcuts
7. `src/BrowserHost/MainWindow.xaml.cs` - Ctrl+F5 shortcut

## Testing Instructions (Windows Required)

To verify the fix works:

1. **Build and run the application on Windows**
2. **Test '@' character typing:**
   - Navigate to any text input field in a webpage
   - Try typing the '@' character using your keyboard's method:
     - On US keyboards: Shift+2
     - On many international keyboards: Ctrl+Alt+2 or AltGr+2
   - The '@' character should now appear instead of switching to workspace 2

3. **Verify existing shortcuts still work:**
   - `Ctrl+1` through `Ctrl+9`: Should switch workspaces
   - `Ctrl+Shift+1` through `Ctrl+Shift+9`: Should move current tab to workspace
   - `Ctrl+T`: Should open action dialog
   - `Ctrl+X`: Should close current tab
   - `Ctrl+B`: Should toggle tab bookmark
   - `Ctrl+G`: Should toggle tab folder
   - `Ctrl+P`: Should toggle tab pinning
   - `Ctrl+MouseWheel`: Should zoom in/out
   - `Ctrl+Backspace`: Should reset zoom
   - `Ctrl+F5`: Should reload page ignoring cache

4. **Test other Alt combinations don't interfere:**
   - `Alt+1`, `Alt+2`, etc. should not trigger workspace shortcuts
   - Other Alt+key combinations should work normally for webpage navigation

## Technical Details

The root cause was in the use of `ModifierKeys.HasFlag()` which performs a bitwise AND operation. This means:
- `ModifierKeys.Control.HasFlag(ModifierKeys.Control)` = true
- `(ModifierKeys.Control | ModifierKeys.Alt).HasFlag(ModifierKeys.Control)` = true (problematic)

By changing to exact equality (`==`), we ensure only the exact modifier combination triggers the shortcut:
- `ModifierKeys.Control == ModifierKeys.Control` = true
- `(ModifierKeys.Control | ModifierKeys.Alt) == ModifierKeys.Control` = false (correct)

This fix prevents any keyboard shortcut conflicts while maintaining all existing functionality.