# Tab Folder Functionality Test Cases

This document outlines the test cases for the new tab folder functionality.

## Backend Tests

### 1. Ctrl-G Keyboard Shortcut
**Test**: Press Ctrl-G on a bookmarked tab
**Expected**: 
- If tab is not in a folder: creates a new folder containing only that tab
- If tab is in a folder: removes tab from folder and places it directly below the folder
- If tab is the last in a folder: removes the entire folder

### 2. Folder Data Persistence
**Test**: Create folders, restart application
**Expected**: Folders are restored from workspace storage with correct names and tab ranges

### 3. Backwards Compatibility  
**Test**: Load workspace file without folder data
**Expected**: Application loads successfully with empty folders array

## Frontend Tests

### 1. Folder UI Rendering
**Test**: Load workspace with folders
**Expected**:
- Folder headers display with correct names
- Open/closed folder icons show appropriate state
- Tabs within folders are indented
- Closed folders hide contained tabs (except first)

### 2. Folder Toggle
**Test**: Click folder header or folder icon
**Expected**: Folder opens/closes, revealing/hiding contained tabs

### 3. Folder Name Editing
**Test**: 
1. Click edit button on folder header
2. Modify name
3. Press Enter or click away
**Expected**: 
- Inline editing field appears
- Changes are saved to backend
- Folder name updates in UI

**Test**: Press Escape during editing
**Expected**: Changes are cancelled, original name restored

### 4. Drag and Drop Validation
**Test**: Attempt to drop tab in middle of closed folder
**Expected**: Drop is prevented/ignored

## Integration Tests

### 1. Full Workflow
**Test**: 
1. Create several bookmarked tabs
2. Select a tab and press Ctrl-G
3. Rename the created folder
4. Add another tab to the folder using Ctrl-G
5. Toggle folder open/closed
6. Remove tab from folder using Ctrl-G

**Expected**: All operations work smoothly with proper UI feedback

## Test Data Scenarios

### Scenario 1: Simple Folder
```json
{
  "folders": [
    {
      "id": "folder-1",
      "name": "Work Tabs", 
      "startIndex": 0,
      "endIndex": 2
    }
  ],
  "tabs": [
    {"id": "tab-1", "title": "GitHub"},
    {"id": "tab-2", "title": "Slack"},
    {"id": "tab-3", "title": "Email"},
    {"id": "tab-4", "title": "News"}
  ]
}
```

### Scenario 2: Multiple Folders
```json
{
  "folders": [
    {
      "id": "folder-1", 
      "name": "Development",
      "startIndex": 0,
      "endIndex": 1  
    },
    {
      "id": "folder-2",
      "name": "Research", 
      "startIndex": 3,
      "endIndex": 4
    }
  ],
  "tabs": [
    {"id": "tab-1", "title": "GitHub"},
    {"id": "tab-2", "title": "Stack Overflow"},
    {"id": "tab-3", "title": "Random Tab"},
    {"id": "tab-4", "title": "Wikipedia"},
    {"id": "tab-5", "title": "Documentation"}
  ]
}
```

## Notes

- Folders only exist in the persistent/bookmark tab area (not ephemeral)
- Folders cannot be nested (single level only)
- Empty folders are automatically removed
- New folders are created in an open state by default
- Folder boundaries are respected during drag operations