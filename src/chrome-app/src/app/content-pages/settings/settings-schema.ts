import { SettingField } from './settingsApi';

export const settingsSchema: SettingField[] = [
  {
    key: 'userAgent',
    type: 'string',
    name: 'User Agent',
    description: 'Controls the user agent string sent by the browser.',
    placeholder: '',
  },
];
