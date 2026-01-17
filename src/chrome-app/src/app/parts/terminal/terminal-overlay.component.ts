import {
  Component,
  OnInit,
  OnDestroy,
  ElementRef,
  ViewChild,
  AfterViewInit,
  signal,
  PLATFORM_ID,
  inject,
} from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { exposeApiToBackend, loadBackendApi } from '../interfaces/api';
import { TerminalApi } from './terminalApi';

// Types for xterm - dynamically imported in browser only
type XtermTerminal = import('@xterm/xterm').Terminal;
type XtermFitAddon = import('@xterm/addon-fit').FitAddon;

interface TerminalBuffer {
  terminal: XtermTerminal;
  fitAddon: XtermFitAddon;
}

@Component({
  selector: 'terminal-overlay',
  standalone: true,
  imports: [CommonModule],
  template: `
    <section class="flex h-full w-full flex-col bg-slate-950/40">
      <div #terminalContainer class="flex-1 overflow-hidden px-2 pt-2"></div>
      <div class="px-2 pb-2">
        <input
          #commandInput
          type="text"
          class="w-full rounded bg-black/20 px-3 py-1 text-xs text-slate-400 border-none placeholder:text-slate-600 outline-none! ring-0!"
          placeholder="Type /clear to clear"
          (keydown.enter)="onCommand(commandInput.value); commandInput.value = ''"
          spellcheck="false"
        />
      </div>
    </section>
  `,
  styles: `
    :host {
      display: block;
      height: 100%;
    }
    :host ::ng-deep .xterm,
    :host ::ng-deep .xterm-viewport,
    :host ::ng-deep .xterm-screen {
      background: transparent !important;
    }
  `,
})
export default class TerminalOverlayComponent
  implements OnInit, AfterViewInit, OnDestroy
{
  @ViewChild('terminalContainer', { static: true })
  terminalContainer!: ElementRef<HTMLDivElement>;

  currentTabId = signal<string | null>(null);
  private terminals = new Map<string, TerminalBuffer>();
  private api!: TerminalApi;
  private resizeObserver?: ResizeObserver;
  private platformId = inject(PLATFORM_ID);
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  private TerminalClass?: any;
  // eslint-disable-next-line @typescript-eslint/no-explicit-any
  private FitAddonClass?: any;

  async ngOnInit() {
    if (!isPlatformBrowser(this.platformId)) return;

    // Dynamically import xterm modules only in browser
    const [xtermModule, fitModule] = await Promise.all([
      import('@xterm/xterm'),
      import('@xterm/addon-fit'),
    ]);
    this.TerminalClass = xtermModule.Terminal;
    this.FitAddonClass = fitModule.FitAddon;

    this.api = await loadBackendApi<TerminalApi>('terminalApi');

    exposeApiToBackend({
      initTerminal: (tabId: string) => {
        this.switchToTab(tabId);
      },
      writeTerminalOutput: (tabId: string, output: string, isError: boolean) => {
        this.writeToTerminal(tabId, output, isError);
      },
      clearTerminal: (tabId: string) => {
        this.clearTerminal(tabId);
      },
      setTerminalVisibility: (visible: boolean) => {
        if (visible) {
          this.refreshCurrentTerminal();
        }
      },
    });
  }

  ngAfterViewInit() {
    if (!isPlatformBrowser(this.platformId)) return;

    this.resizeObserver = new ResizeObserver(() => {
      this.fitCurrentTerminal();
    });
    this.resizeObserver.observe(this.terminalContainer.nativeElement);
  }

  ngOnDestroy() {
    this.resizeObserver?.disconnect();
    this.terminals.forEach((buf) => buf.terminal.dispose());
    this.terminals.clear();
  }

  private getOrCreateTerminal(tabId: string): TerminalBuffer | null {
    if (!this.TerminalClass || !this.FitAddonClass) return null;

    let buffer = this.terminals.get(tabId);
    if (!buffer) {
      const terminal = new this.TerminalClass({
        theme: {
          background: 'transparent',
          foreground: '#94a3b8', // slate-400
          cursor: '#64748b', // slate-500
          cursorAccent: 'transparent',
          selectionBackground: '#334155',
          black: '#1e293b',
          red: '#f87171',
          green: '#4ade80',
          yellow: '#fbbf24',
          blue: '#60a5fa',
          magenta: '#a78bfa',
          cyan: '#22d3ee',
          white: '#cbd5e1',
          brightBlack: '#475569',
          brightRed: '#fca5a5',
          brightGreen: '#86efac',
          brightYellow: '#fde047',
          brightBlue: '#93c5fd',
          brightMagenta: '#c4b5fd',
          brightCyan: '#67e8f9',
          brightWhite: '#f1f5f9',
        },
        fontSize: 12,
        fontFamily: 'Consolas, "Courier New", monospace',
        cursorBlink: true,
        cursorStyle: 'bar',
        scrollback: 5000,
        convertEol: true,
        allowTransparency: true,
      });

      const fitAddon = new this.FitAddonClass();
      terminal.loadAddon(fitAddon);

      buffer = { terminal, fitAddon };
      this.terminals.set(tabId, buffer);
    }
    return buffer;
  }

  private switchToTab(tabId: string) {
    // Hide all terminals
    this.terminals.forEach((buf) => {
      if (buf.terminal.element?.parentElement) {
        buf.terminal.element.style.display = 'none';
      }
    });

    this.currentTabId.set(tabId);
    const buffer = this.getOrCreateTerminal(tabId);
    if (!buffer) return;

    if (!buffer.terminal.element?.parentElement) {
      buffer.terminal.open(this.terminalContainer.nativeElement);
    }

    if (buffer.terminal.element) {
      buffer.terminal.element.style.display = '';
    }

    // Fit after a brief delay to ensure DOM is ready
    setTimeout(() => buffer.fitAddon.fit(), 10);
  }

  private writeToTerminal(tabId: string, output: string, isError: boolean) {
    const buffer = this.getOrCreateTerminal(tabId);
    if (!buffer) return;
    const prefix = isError ? '\x1b[31m' : ''; // Red for errors
    const suffix = isError ? '\x1b[0m' : '';
    buffer.terminal.writeln(`${prefix}${output}${suffix}`);
  }

  private clearTerminal(tabId: string) {
    const buffer = this.terminals.get(tabId);
    if (buffer) {
      buffer.terminal.clear();
    }
  }

  private fitCurrentTerminal() {
    const tabId = this.currentTabId();
    if (tabId) {
      const buffer = this.terminals.get(tabId);
      if (buffer) {
        try {
          buffer.fitAddon.fit();
        } catch {
          // Ignore fit errors during transitions
        }
      }
    }
  }

  private refreshCurrentTerminal() {
    const tabId = this.currentTabId();
    if (tabId) {
      const buffer = this.terminals.get(tabId);
      if (buffer) {
        try {
          buffer.fitAddon.fit();
          // Force full refresh to fix background rendering
          buffer.terminal.refresh(0, buffer.terminal.rows - 1);
        } catch {
          // Ignore errors during transitions
        }
      }
    }
  }

  async onCommand(value: string) {
    const trimmed = value.trim().toLowerCase();
    const tabId = this.currentTabId();

    if (trimmed === '/clear' && tabId) {
      this.clearTerminal(tabId);
      await this.api.clearTerminal(tabId);
    }
  }
}
