import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FaviconComponent } from './favicon.component';

describe('FaviconComponent', () => {
  let component: FaviconComponent;
  let fixture: ComponentFixture<FaviconComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FaviconComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(FaviconComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should show fallback when no src provided', () => {
    fixture.detectChanges();
    expect(component.showFallback()).toBe(true);
    
    const compiled = fixture.nativeElement as HTMLElement;
    const img = compiled.querySelector('img');
    expect(img?.src).toContain('data:image/svg+xml');
  });

  it('should show provided favicon when src is given', () => {
    fixture.componentRef.setInput('src', 'https://example.com/favicon.ico');
    fixture.detectChanges();
    
    expect(component.showFallback()).toBe(false);
    
    const compiled = fixture.nativeElement as HTMLElement;
    const img = compiled.querySelector('img');
    expect(img?.src).toContain('example.com/favicon.ico');
  });

  it('should apply custom CSS class', () => {
    fixture.componentRef.setInput('cssClass', 'custom-class');
    fixture.detectChanges();
    
    const compiled = fixture.nativeElement as HTMLElement;
    const img = compiled.querySelector('img');
    expect(img?.className).toBe('custom-class');
  });

  it('should switch to fallback on load error', () => {
    fixture.componentRef.setInput('src', 'https://example.com/nonexistent.ico');
    fixture.detectChanges();
    
    expect(component.showFallback()).toBe(false);
    
    // Simulate image load error
    component.onLoadError();
    fixture.detectChanges();
    
    expect(component.showFallback()).toBe(true);
    
    const compiled = fixture.nativeElement as HTMLElement;
    const img = compiled.querySelector('img');
    expect(img?.src).toContain('data:image/svg+xml');
  });
});