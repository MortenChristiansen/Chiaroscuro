import { Component, input } from '@angular/core';
import { FaIconComponent } from '@fortawesome/angular-fontawesome';
import { faFolder, faFolderOpen } from '@fortawesome/free-regular-svg-icons';

@Component({
  selector: 'folder-icon',
  imports: [FaIconComponent],
  styles: [
    `
      :host {
        position: relative;
        display: inline-flex;
        align-items: center;
        justify-content: center;
        width: 1.5rem;
        height: 1.5rem;
      }

      .folder-icon {
        position: absolute;
        padding-top: 0.25rem;
        font-size: 1rem;
        transition: opacity 200ms cubic-bezier(0.4, 0, 0.2, 1),
          transform 200ms cubic-bezier(0.4, 0, 0.2, 1);
      }

      .folder-icon-open {
        opacity: 0;
        transform: translateY(-0.125rem) scale(0.98);
      }

      .folder-icon-closed {
        opacity: 1;
        transform: translateY(0);
      }

      :host(.open) .folder-icon-open {
        opacity: 1;
        transform: translateY(0);
      }

      :host(.open) .folder-icon-closed {
        opacity: 0;
        transform: translateY(0.125rem) scale(0.98);
      }
    `,
  ],
  template: `
    <fa-icon class="folder-icon folder-icon-open" [icon]="openFolderIcon" />
    <fa-icon class="folder-icon folder-icon-closed" [icon]="closedFolderIcon" />
  `,
  host: {
    '[class.open]': 'isOpen()',
  },
})
export class FolderIconComponent {
  isOpen = input.required<boolean>();

  protected readonly openFolderIcon = faFolderOpen;
  protected readonly closedFolderIcon = faFolder;
}
