export type SettingPrimitive = string | number | boolean | null;

export type SettingsValues = Record<string, SettingPrimitive>;

export type PlainSettings = Record<string, string | number | boolean>;

export type SettingType = 'string' | 'boolean' | 'integer';

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

export interface BooleanField extends SettingFieldBase {
  type: 'boolean';
}

export interface IntegerField extends SettingFieldBase {
  type: 'integer';
}

export type SettingField = StringField | BooleanField | IntegerField;

export interface SettingsApi {
  settingsPageLoading: () => void;
  saveSettings: (values: PlainSettings) => void;
}
