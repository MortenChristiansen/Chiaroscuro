using System.Collections.Generic;
using System.Text.Json.Serialization;
using BrowserHost.Features.Settings;
using BrowserHost.Features.AppState;
using BrowserHost.Features.ActionContext.PinnedTabs;
using BrowserHost.Features.ActionDialog;
using BrowserHost.Features.TabPalette.DomainCustomization;
using BrowserHost.Features.ActionContext.Workspaces;
using BrowserHost.Features.TabPalette.TabCustomization;
using BrowserHost.Utilities;

namespace BrowserHost.Serialization;

// Context for persisted data (indented output)
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(PersistentData))]
[JsonSerializable(typeof(PersistentData<SettingsDataV1>))]
[JsonSerializable(typeof(PersistentData<AppStateDataV1>))]
[JsonSerializable(typeof(PersistentData<PinnedTabDataV1>))]
[JsonSerializable(typeof(SettingsDataV1))]
[JsonSerializable(typeof(AppStateDataV1))]
[JsonSerializable(typeof(PinnedTabDataV1))]
[JsonSerializable(typeof(PinnedTabDtoV1))]
[JsonSerializable(typeof(NavigationHistoryEntry))]
[JsonSerializable(typeof(Dictionary<string, NavigationHistoryEntry>))]
[JsonSerializable(typeof(PersistentData<DomainCustomizationSettingsV1>))]
[JsonSerializable(typeof(PersistentData<DomainCustomizationSettingsV2>))]
[JsonSerializable(typeof(DomainCustomizationSettingsV1))]
[JsonSerializable(typeof(DomainCustomizationSettingsV2))]
[JsonSerializable(typeof(DomainCustomizationDataV1))]
[JsonSerializable(typeof(PersistentData<WorkspacesDataDtoV1>))]
[JsonSerializable(typeof(WorkspacesDataDtoV1))]
[JsonSerializable(typeof(WorkspaceDtoV1))]
[JsonSerializable(typeof(WorkspaceTabStateDtoV1))]
[JsonSerializable(typeof(FolderDtoV1))]
[JsonSerializable(typeof(PersistentData<TabCustomizationDataV1>))]
[JsonSerializable(typeof(TabCustomizationDataV1))]
public partial class BrowserHostJsonContext : JsonSerializerContext
{
}

// Context for camelCase serialization used in miscellaneous JSON output helpers
[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(object))]
public partial class BrowserHostCamelCaseJsonContext : JsonSerializerContext
{
}
