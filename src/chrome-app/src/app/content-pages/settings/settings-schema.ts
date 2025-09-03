import { SettingField } from './settingsApi';

export const settingsSchema: SettingField[] = [
  {
    key: 'userAgent',
    type: 'string',
    name: 'User Agent',
    description: 'Controls the user agent string sent by the browser.',
    placeholder: '',
  },
  {
    key: 'ssoEnabledDomains',
    type: 'string[]',
    name: 'SSO Enabled Domains',
    description:
      'Domains where Single Sign-On (SSO) is enabled by using a WebView2 based browser. All browser features might not be available. All sub domains are included by default.',
    placeholder: 'Enter domain name',
  },
];
