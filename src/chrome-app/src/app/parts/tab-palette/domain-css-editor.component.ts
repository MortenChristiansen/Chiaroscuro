import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';
import {
  faPenToSquare,
  faToggleOff,
  faToggleOn,
  faTrash,
} from '@fortawesome/free-solid-svg-icons';
import { IconButtonComponent } from '../../shared/icon-button.component';
import { exposeApiToBackend, loadBackendApi } from '../interfaces/api';
import { DomainCustomizationApi } from './domainCustomizationApi';

@Component({
  selector: 'domain-css-editor',
  standalone: true,
  imports: [CommonModule, IconButtonComponent, FaIconComponent],
  template: `
    <div class="flex flex-col gap-2 w-full">
      <div class="text-xs text-gray-400">Custom domain styling</div>
      @if (currentDomain()) {
      <div class="text-xs text-gray-500 mb-2">
        Domain: {{ currentDomain() }}
      </div>
      <div class="flex items-center gap-2">
        <icon-button
          title="Edit CSS for this domain"
          (click)="editCss()"
          [disabled]="!currentDomain()"
        >
          <fa-icon [icon]="editCssIcon" />
        </icon-button>

        @if (hasCustomCss()) {
        <icon-button
          title="Toggle CSS enabled/disabled"
          (click)="toggleCssEnabled()"
          [class]="cssEnabled() ? 'text-green-400' : 'text-gray-500'"
        >
          <fa-icon [icon]="cssEnabled() ? cssEnabledIcon : cssDisabledIcon" />
        </icon-button>

        <icon-button
          title="Remove custom CSS"
          (click)="removeCss()"
          class="text-red-400"
        >
          <fa-icon [icon]="removeCssIcon" />
        </icon-button>
        }
      </div>

      <div class="text-xs text-gray-500 h-4">
        @if (hasCustomCss()) { @if (cssEnabled()) { CSS is enabled for this
        domain } @else { CSS is disabled for this domain } } @else { No custom
        CSS for this domain }
      </div>
      } @else {
      <div class="text-xs text-gray-500">No domain detected</div>
      }
    </div>
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
