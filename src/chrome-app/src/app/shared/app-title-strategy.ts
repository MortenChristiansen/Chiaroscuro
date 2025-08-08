import { DOCUMENT } from '@angular/common';
import { Inject, Injectable } from '@angular/core';
import {
  ActivatedRouteSnapshot,
  RouterStateSnapshot,
  TitleStrategy,
} from '@angular/router';

@Injectable()
export class AppTitleStrategy extends TitleStrategy {
  constructor(@Inject(DOCUMENT) private readonly doc: Document) {
    super();
  }

  override updateTitle(snapshot: RouterStateSnapshot): void {
    const title = this.buildTitle(snapshot);
    if (title !== undefined) {
      this.doc.title = title;
    }

    const primary = this.getPrimary(snapshot.root);
    const favicon = (primary?.data as any)?.['favicon'] as string | undefined;
    if (favicon) {
      this.setFavicon(favicon);
    }
  }

  private getPrimary(
    route: ActivatedRouteSnapshot
  ): ActivatedRouteSnapshot | null {
    let current: ActivatedRouteSnapshot | null = route;
    while (current) {
      if (current.firstChild) {
        current = current.firstChild;
      } else {
        break;
      }
    }
    return current;
  }

  private setFavicon(href: string): void {
    const head = this.doc.head || this.doc.getElementsByTagName('head')[0];
    let link = head.querySelector(
      "link[rel*='icon']"
    ) as HTMLLinkElement | null;
    if (!link) {
      link = this.doc.createElement('link');
      link.rel = 'icon';
      link.type = 'image/svg+xml';
      head.appendChild(link);
    }
    link.href = href;
  }
}
