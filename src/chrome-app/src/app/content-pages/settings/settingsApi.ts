export type SettingPrimitive = string | number | boolean | null | string[];

export type SettingsValues = Record<string, SettingPrimitive>;

// Plain settings now also allow string[] for array-based values.
export type PlainSettings = Record<
  string,
  string | number | boolean | string[]
>;

export type SettingType = 'string' | 'string[]' | 'boolean' | 'integer';

export interface SettingFieldBase {
  key: string;
  name: string;
  description: string;
  defaultValue?: SettingPrimitive;
}

export interface StringField extends SettingFieldBase {
  type: 'string';
  placeholder?: string;
}

export interface StringArrayField extends SettingFieldBase {
  type: 'string[]';
  placeholder?: string;
}

export interface BooleanField extends SettingFieldBase {
  type: 'boolean';
}

export interface IntegerField extends SettingFieldBase {
  type: 'integer';
}

export type SettingField =
  | StringField
  | BooleanField
  | IntegerField
  | StringArrayField;

export interface SettingsApi {
  settingsPageLoading: () => void;
  saveSettings: (values: PlainSettings) => void;
}
