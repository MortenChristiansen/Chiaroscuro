import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';
import {
  faPaintBrush,
  faPenToSquare,
  faToggleOff,
  faToggleOn,
  faTrash,
} from '@fortawesome/free-solid-svg-icons';
import { IconButtonComponent } from '../../../shared/icon-button.component';
import { exposeApiToBackend, loadBackendApi } from '../../interfaces/api';
import { DomainCustomizationApi } from '../domainCustomizationApi';
import { TabPaletteControlSectionComponent } from '../containers/control-section.component';

@Component({
  selector: 'domain-css-editor',
  imports: [
    CommonModule,
    IconButtonComponent,
    FaIconComponent,
    TabPaletteControlSectionComponent,
  ],
  template: `
    <tab-palette-control-section
      [title]="'Custom domain styling'"
      [icon]="stylingIcon"
    >
      @if (currentDomain()) {
      <div
        class="rounded-lg border border-slate-800/70 bg-slate-950/40 p-3 shadow-inner"
      >
        <div class="flex flex-wrap items-center gap-2">
          <icon-button
            title="Edit CSS for this domain"
            (click)="editCss()"
            [disabled]="!currentDomain()"
          >
            <fa-icon class="text-sky-300" [icon]="editCssIcon" />
          </icon-button>

          @if (hasCustomCss()) {
          <icon-button
            title="Toggle CSS enabled/disabled"
            (click)="toggleCssEnabled()"
          >
            <fa-icon
              [class.text-emerald-300]="cssEnabled()"
              [class.text-amber-300]="!cssEnabled()"
              [icon]="cssEnabled() ? cssEnabledIcon : cssDisabledIcon"
            />
          </icon-button>

          <icon-button title="Remove custom CSS" (click)="removeCss()">
            <fa-icon class="text-rose-300" [icon]="removeCssIcon" />
          </icon-button>
          }
        </div>

        <div class="mt-3 text-xs">
          @if (hasCustomCss()) {
          <span
            [class.text-emerald-400]="cssEnabled()"
            [class.text-amber-300]="!cssEnabled()"
          >
            @if (cssEnabled()) { Custom CSS is live for this domain. } @else {
            Custom CSS is saved but currently disabled. }
          </span>
          } @else {
          <span class="text-slate-500">No custom CSS has been saved yet.</span>
          }
        </div>
      </div>
      } @else {
      <div
        class="rounded-lg border border-slate-800/70 bg-slate-950/40 p-3 text-xs text-slate-500"
      >
        No domain detected. Navigate to a site to unlock domain styling tools.
      </div>
      }
    </tab-palette-control-section>
  `,
})
export class DomainCssEditorComponent implements OnInit {
  currentDomain = signal<string | null>(null);
  cssEnabled = signal(false);
  hasCustomCss = signal(false);

  protected readonly editCssIcon = faPenToSquare;
  protected readonly cssEnabledIcon = faToggleOn;
  protected readonly cssDisabledIcon = faToggleOff;
  protected readonly removeCssIcon = faTrash;
  protected readonly stylingIcon = faPaintBrush;

  private api!: DomainCustomizationApi;

  async ngOnInit() {
    this.api = await loadBackendApi<DomainCustomizationApi>(
      'domainCustomizationApi'
    );

    exposeApiToBackend({
      initDomainSettings: (
        domain: string,
        enabled: boolean,
        hasCustomCss: boolean
      ) => {
        this.currentDomain.set(domain);
        this.cssEnabled.set(enabled);
        this.hasCustomCss.set(hasCustomCss);
      },
      updateDomainSettings: (
        domain: string,
        enabled: boolean,
        hasCustomCss: boolean
      ) => {
        this.currentDomain.set(domain);
        this.cssEnabled.set(enabled);
        this.hasCustomCss.set(hasCustomCss);
      },
    });
  }

  async editCss() {
    if (this.currentDomain()) {
      await this.api.editCss();
    }
  }

  async toggleCssEnabled() {
    if (this.currentDomain()) {
      const newEnabled = !this.cssEnabled();
      await this.api.setCssEnabled(newEnabled);
    }
  }

  async removeCss() {
    if (this.currentDomain() && this.hasCustomCss()) {
      await this.api.removeCss();
    }
  }
}
