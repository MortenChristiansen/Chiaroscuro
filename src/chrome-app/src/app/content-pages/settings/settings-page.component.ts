import { Component, OnInit, computed, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { exposeApiToBackend, loadBackendApi } from '../../parts/interfaces/api';
import {
  SettingField,
  SettingsApi,
  SettingsValues,
  PlainSettings,
} from './settingsApi.js';
import { settingsSchema } from './settings-schema';

@Component({
  selector: 'settings-page',
  imports: [CommonModule, FormsModule],
  template: `
    <div
      class="settings-page w-full h-full text-white font-sans  bg-gray-900/90"
    >
      <div
        class="sticky top-0 z-10 flex items-center justify-between px-6 py-4 bg-gray-900/70 backdrop-blur border-b border-white/10"
      >
        <h1 class="text-xl font-semibold">Settings</h1>

        <div class="flex items-center gap-2">
          <button
            class="px-3 py-1.5 rounded bg-white/10 hover:bg-white/20 disabled:opacity-50 disabled:cursor-not-allowed"
            [disabled]="!isDirty()"
            (click)="save()"
          >
            Save
          </button>
          <button
            class="px-3 py-1.5 rounded bg-white/5 hover:bg-white/10 disabled:opacity-50 disabled:cursor-not-allowed"
            [disabled]="!isDirty()"
            (click)="reset()"
          >
            Reset
          </button>
        </div>
      </div>

      <div class="px-6 py-4 flex flex-col gap-4">
        @for (field of schema; track field.key) {
        <div class="setting-row">
          <div
            class="flex items-start gap-6 p-4 rounded-lg bg-white/5 hover:bg-white/10 transition-colors"
          >
            <div class="flex-1 min-w-0">
              <div class="text-sm font-medium">{{ field.name }}</div>
              <div class="text-xs text-gray-400 mt-1">
                {{ field.description }}
              </div>
            </div>

            <div class="flex-shrink-0 basis-1/2">
              @if (field.type === 'string') {
              <input
                type="text"
                class="w-full px-2 py-1.5 rounded bg-gray-800 border border-white/10 focus:outline-none focus:ring-2 focus:ring-blue-500"
                [ngModel]="asString(getValue(field.key))"
                (ngModelChange)="onStringChange(field.key, $event)"
                [placeholder]="field.placeholder ?? ''"
              />
              } @else if (field.type === 'integer') {
              <input
                type="number"
                class="w-full px-2 py-1.5 rounded bg-gray-800 border border-white/10 focus:outline-none focus:ring-2 focus:ring-blue-500"
                [ngModel]="asNumber(getValue(field.key))"
                (ngModelChange)="onIntegerChange(field.key, $event)"
                step="1"
              />
              } @else if (field.type === 'boolean') {
              <label class="inline-flex items-center gap-2 select-none w-auto">
                <input
                  type="checkbox"
                  class="w-4 h-4 rounded border-white/10 bg-gray-800"
                  [ngModel]="asBoolean(getValue(field.key))"
                  (ngModelChange)="onBooleanChange(field.key, $event)"
                />
                <span class="text-sm text-gray-300">Enabled</span>
              </label>
              }
            </div>
          </div>
        </div>
        }
      </div>
    </div>
  `,
  styles: ``,
})
export default class SettingsPageComponent implements OnInit {
  schema = settingsSchema;

  private savedValues = signal<SettingsValues>({});
  currentValues = signal<SettingsValues>({});

  api!: SettingsApi;

  isDirty = computed(() => {
    const a = this.savedValues();
    const b = this.currentValues();
    const keys = new Set([...Object.keys(a), ...Object.keys(b)]);
    for (const k of keys) {
      if (a[k] !== b[k]) return true;
    }
    return false;
  });

  async ngOnInit() {
    this.api = await loadBackendApi<SettingsApi>('settingsApi');

    this.api.settingsPageLoading();

    exposeApiToBackend({
      settingsLoaded: (plain: PlainSettings) => {
        const values = this.fromPlain(plain);
        const normalized = this.withSchemaDefaults(values);
        this.savedValues.set(normalized);
        this.currentValues.set({ ...normalized });
      },
    });
  }

  asString(v: unknown): string {
    return typeof v === 'string' ? v : '';
  }
  asNumber(v: unknown): number | null {
    return typeof v === 'number' && Number.isInteger(v) ? v : null;
  }
  asBoolean(v: unknown): boolean {
    return typeof v === 'boolean' ? v : false;
  }

  getValue(key: string) {
    const curr = this.currentValues();
    return key in curr ? curr[key] : this.defaultForKey(key);
  }

  onStringChange(key: string, value: string) {
    this.setValue(key, value);
  }
  onIntegerChange(key: string, value: any) {
    const n =
      value === '' || value === null || value === undefined
        ? null
        : Number.parseInt(String(value), 10);
    this.setValue(key, Number.isFinite(n as number) ? (n as number) : null);
  }
  onBooleanChange(key: string, value: boolean) {
    this.setValue(key, !!value);
  }

  setValue(key: string, value: string | number | boolean | null) {
    this.currentValues.update((curr) => ({ ...curr, [key]: value }));
  }

  save() {
    const values = this.currentValues();
    const plain = this.toPlain(values);
    this.savedValues.set({ ...values });
    this.api.saveSettings(plain);
  }

  reset() {
    this.currentValues.set({ ...this.savedValues() });
  }

  private withSchemaDefaults(values: SettingsValues): SettingsValues {
    const result: SettingsValues = { ...values };
    for (const f of this.schema) {
      if (!(f.key in result)) {
        result[f.key] = this.defaultForField(f);
      }
    }
    return result;
  }

  private defaultForKey(key: string) {
    const f = this.schema.find((s) => s.key === key);
    return f ? this.defaultForField(f) : null;
  }

  private defaultForField(field: SettingField) {
    if (field.defaultValue !== undefined) return field.defaultValue;
    switch (field.type) {
      case 'string':
        return '';
      case 'boolean':
        return false;
      case 'integer':
        return 0;
      default:
        return null;
    }
  }

  // Convert from a PlainSettings object (only string|number|boolean) to SettingsValues
  private fromPlain(plain: PlainSettings): SettingsValues {
    const result: SettingsValues = {};
    const keys = new Set([
      ...this.schema.map((f) => f.key),
      ...Object.keys(plain ?? {}),
    ]);
    for (const key of keys) {
      const field = this.schema.find((f) => f.key === key);
      const value = plain[key];
      if (value === undefined) {
        result[key] = this.defaultForKey(key);
        continue;
      }
      if (field?.type === 'integer') {
        // Only keep integers; otherwise null
        const n = Number(value);
        result[key] = Number.isInteger(n) ? n : null;
      } else if (field?.type === 'boolean') {
        result[key] = Boolean(value);
      } else if (field?.type === 'string') {
        // Preserve nulls rather than turning them into the string "null"
        result[key] = value === null ? null : String(value);
      } else {
        // If schema unknown, attempt best-effort mapping
        if (typeof value === 'string' || typeof value === 'boolean') {
          result[key] = value;
        } else if (typeof value === 'number' && Number.isFinite(value)) {
          result[key] = value;
        } else {
          result[key] = null;
        }
      }
    }
    return result;
  }

  // Convert SettingsValues back to PlainSettings, dropping nulls
  private toPlain(values: SettingsValues): PlainSettings {
    const result: PlainSettings = {};
    for (const f of this.schema) {
      const v = values[f.key];
      if (v === null || v === undefined) continue;
      if (f.type === 'integer') {
        // ensure integer
        const n = Number(v);
        if (Number.isInteger(n)) result[f.key] = n;
      } else if (f.type === 'boolean') {
        result[f.key] = Boolean(v);
      } else if (f.type === 'string') {
        result[f.key] = String(v);
      }
    }
    return result;
  }
}
