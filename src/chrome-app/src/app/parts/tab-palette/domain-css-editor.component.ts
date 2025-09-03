import { CommonModule } from '@angular/common';
import { Component, OnInit, signal } from '@angular/core';
import { IconButtonComponent } from '../../shared/icon-button.component';
import { exposeApiToBackend, loadBackendApi } from '../interfaces/api';
import { DomainCustomizationApi } from './domainCustomizationApi';

@Component({
  selector: 'domain-css-editor',
  standalone: true,
  imports: [CommonModule, IconButtonComponent],
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
            <svg
              xmlns="http://www.w3.org/2000/svg"
              viewBox="0 0 20 20"
              fill="currentColor"
              width="20"
              height="20"
            >
              <path d="M13.586 3.586a2 2 0 112.828 2.828l-.793.793-2.828-2.828.793-.793zM11.379 5.793L3 14.172V17h2.828l8.379-8.379-2.828-2.828z" />
            </svg>
          </icon-button>
          
          @if (hasCustomCss()) {
            <icon-button 
              title="Toggle CSS enabled/disabled" 
              (click)="toggleCssEnabled()"
              [class]="cssEnabled() ? 'text-green-400' : 'text-gray-500'"
            >
              <svg
                xmlns="http://www.w3.org/2000/svg"
                viewBox="0 0 20 20"
                fill="currentColor"
                width="20"
                height="20"
              >
                @if (cssEnabled()) {
                  <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zm3.707-9.293a1 1 0 00-1.414-1.414L9 10.586 7.707 9.293a1 1 0 00-1.414 1.414l2 2a1 1 0 001.414 0l4-4z" clip-rule="evenodd" />
                } @else {
                  <path fill-rule="evenodd" d="M10 18a8 8 0 100-16 8 8 0 000 16zM8.707 7.293a1 1 0 00-1.414 1.414L8.586 10l-1.293 1.293a1 1 0 101.414 1.414L10 11.414l1.293 1.293a1 1 0 001.414-1.414L11.414 10l1.293-1.293a1 1 0 00-1.414-1.414L10 8.586 8.707 7.293z" clip-rule="evenodd" />
                }
              </svg>
            </icon-button>
            
            <icon-button 
              title="Remove custom CSS" 
              (click)="removeCss()"
              class="text-red-400"
            >
              <svg
                xmlns="http://www.w3.org/2000/svg"
                viewBox="0 0 20 20"
                fill="currentColor"
                width="20"
                height="20"
              >
                <path fill-rule="evenodd" d="M9 2a1 1 0 00-.894.553L7.382 4H4a1 1 0 000 2v10a2 2 0 002 2h8a2 2 0 002-2V6a1 1 0 100-2h-3.382l-.724-1.447A1 1 0 0011 2H9zM7 8a1 1 0 012 0v6a1 1 0 11-2 0V8zm5-1a1 1 0 00-1 1v6a1 1 0 102 0V8a1 1 0 00-1-1z" clip-rule="evenodd" />
              </svg>
            </icon-button>
          }
        </div>
        
        <div class="text-xs text-gray-500 h-4">
          @if (hasCustomCss()) {
            @if (cssEnabled()) {
              CSS is enabled for this domain
            } @else {
              CSS is disabled for this domain
            }
          } @else {
            No custom CSS for this domain
          }
        </div>
      } @else {
        <div class="text-xs text-gray-500">
          No domain detected
        </div>
      }
    </div>
  `,
})
export class DomainCssEditorComponent implements OnInit {
  currentDomain = signal<string | null>(null);
  cssEnabled = signal(false);
  hasCustomCss = signal(false);

  private api!: DomainCustomizationApi;

  async ngOnInit() {
    this.api = await loadBackendApi<DomainCustomizationApi>('domainCustomizationApi');

    exposeApiToBackend({
      initDomainSettings: (domain: string, enabled: boolean, hasCustomCss: boolean) => {
        this.currentDomain.set(domain);
        this.cssEnabled.set(enabled);
        this.hasCustomCss.set(hasCustomCss);
      },
      updateDomainSettings: (domain: string, enabled: boolean, hasCustomCss: boolean) => {
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