# Picture-in-Picture (PIP) Feature Documentation

## Overview

The PIP feature automatically detects video content playing in browser tabs and displays it in a small, always-on-top window when the user switches away from the video tab.

## Features

### Automatic Video Detection
- Polls all tabs every 2 seconds to detect playing videos
- Uses JavaScript evaluation to check for `<video>` elements that are not paused or ended
- Monitors video state changes (play/pause/end events)

### Smart PIP Window Management
- **Show PIP**: Automatically appears when switching away from a tab with playing video
- **Hide PIP**: Automatically disappears when returning to the video tab
- **Position**: Bottom-right corner of the screen (20px margin from edges)
- **Size**: 320x180 pixels (resizable, minimum 200x120)

### Control Overlay
The PIP window shows control buttons when hovered:
- **❌ Close Button** (top-right): Closes the PIP window
- **↗ Activate Tab Button** (next to close): Switches back to the tab containing the video
- **⏸/▶ Play/Pause Button** (center): Controls video playback

### Window Properties
- **Borderless**: No window chrome for minimal distraction
- **Always on Top**: Stays above all other windows
- **Transparent Background**: Rounded corners with smooth edges
- **Not in Taskbar**: Doesn't clutter the taskbar

## Architecture

### Core Components

1. **PIPFeature** (`Features/PIP/PIPFeature.cs`)
   - Main feature class extending the base `Feature` class
   - Manages video detection polling and tab event handling
   - Coordinates PIP window lifecycle

2. **PIPWindow** (`Features/PIP/PIPWindow.xaml/.cs`)
   - WPF window for the PIP display
   - Handles control overlay animations
   - Manages video state synchronization

3. **Integration Points**
   - Registered in `MainWindow.xaml.cs` with other features
   - Uses existing `TabActivatedEvent` and `TabClosedEvent` events
   - Extends `ActionContextBrowserExtensions` with `ActivateTab` method

### Event Flow

1. **Video Detection**: Timer-based polling checks active tab for playing videos
2. **Tab Switch Detection**: `TabActivatedEvent` triggers PIP show/hide logic
3. **Control Actions**: User interactions in PIP window trigger tab operations
4. **Cleanup**: `TabClosedEvent` ensures PIP is hidden when video tab closes

## Testing

### Manual Testing Steps

1. **Create Test Video Content**
   - Open `/tmp/pip-test-video.html` in the browser
   - Click "Create Test Video" to generate a test video
   - Click "Play Video" to start playback

2. **Test PIP Activation**
   - Ensure video is playing
   - Open a new tab or switch to existing tab
   - Verify PIP window appears in bottom-right corner

3. **Test PIP Controls**
   - Hover over PIP window to see control overlay
   - Test close button (❌) - should close PIP
   - Test activate tab button (↗) - should switch back to video tab
   - Test play/pause button (⏸/▶) - should control video playback

4. **Test PIP Deactivation**
   - Switch back to video tab
   - Verify PIP window disappears automatically

### Automated Testing

The Angular frontend includes basic tests:
```bash
cd src/chrome-app
npm test
```

## Implementation Notes

### Video Detection Strategy
- **Polling Approach**: Uses timer-based polling instead of event-driven detection for reliability
- **JavaScript Evaluation**: Executes scripts in tab browsers to check video state
- **Cross-Origin Safe**: Works with videos from any domain

### Performance Considerations
- **2-Second Polling Interval**: Balances responsiveness with resource usage
- **Single Active PIP**: Only one PIP window active at a time
- **Lightweight Window**: Minimal UI elements and efficient rendering

### Browser Compatibility
- **CefSharp Integration**: Works with the CefSharp browser engine
- **Video Element Support**: Compatible with HTML5 `<video>` elements
- **Modern Web Standards**: Supports videos loaded via JavaScript/DOM manipulation

## Future Enhancements

### Potential Improvements
1. **True Video Capture**: Instead of placeholder content, capture actual video frames
2. **Multiple Video Support**: Handle multiple videos in the same tab
3. **Video Quality Options**: Allow users to choose PIP video quality
4. **Persistent Settings**: Remember user preferences for PIP behavior
5. **Keyboard Shortcuts**: Add hotkeys for PIP control
6. **Custom Positioning**: Allow users to move/resize PIP window

### Technical Debt
1. **Error Handling**: Add more robust error handling for edge cases
2. **Resource Cleanup**: Ensure all timers and resources are properly disposed
3. **Configuration**: Make polling interval and window size configurable
4. **Logging**: Add structured logging for debugging and monitoring

## Integration with Existing Features

### Features Integration
- **TabsFeature**: Uses existing tab management and event system
- **ActionDialog**: Compatible with existing browser API patterns
- **CustomWindowChrome**: Follows established window management patterns

### Code Patterns
- **Feature Registration**: Follows same pattern as DevTool, Zoom, etc.
- **Event System**: Uses existing PubSub pattern for loose coupling
- **Extension Methods**: Adds to existing ActionContextBrowser extensions
- **XAML Styling**: Consistent with application's design language

## Configuration

### Default Settings
```csharp
// Video detection polling interval
private Timer _videoPollingTimer = new Timer(2000); // 2 seconds

// PIP window dimensions
Width = 320, Height = 180, MinWidth = 200, MinHeight = 120

// PIP window position (bottom-right with 20px margins)
Left = workingArea.Right - Width - 20;
Top = workingArea.Bottom - Height - 20;

// Control overlay fade timing
HideControlsTimer.Interval = TimeSpan.FromSeconds(3);
```

These settings can be made configurable in future iterations based on user feedback.