import { ComponentFixture, TestBed } from '@angular/core/testing';
import { TabsListFolderComponent } from './tab-list-folder.component';
import { signal } from '@angular/core';

describe('TabsListFolderComponent', () => {
  let component: TabsListFolderComponent;
  let fixture: ComponentFixture<TabsListFolderComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [TabsListFolderComponent],
    }).compileComponents();

    fixture = TestBed.createComponent(TabsListFolderComponent);
    component = fixture.componentInstance;
    
    // Set required inputs
    fixture.componentRef.setInput('name', 'Test Folder');
    fixture.componentRef.setInput('isOpen', true);
    fixture.componentRef.setInput('isNew', false);
    fixture.componentRef.setInput('containsActiveTab', false);
    fixture.componentRef.setInput('isDragging', false);
    
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should allow editing when not dragging', () => {
    // Arrange
    fixture.componentRef.setInput('isDragging', false);
    const mockEvent = new Event('click');
    spyOn(mockEvent, 'stopPropagation');
    spyOn(component.isEditing, 'set');

    // Act
    component.onEditButtonClick(mockEvent);

    // Assert
    expect(mockEvent.stopPropagation).toHaveBeenCalled();
    expect(component.isEditing.set).toHaveBeenCalledWith(true);
  });

  it('should prevent editing when dragging', () => {
    // Arrange
    fixture.componentRef.setInput('isDragging', true);
    const mockEvent = new Event('click');
    spyOn(mockEvent, 'stopPropagation');
    spyOn(component.isEditing, 'set');

    // Act
    component.onEditButtonClick(mockEvent);

    // Assert
    expect(mockEvent.stopPropagation).not.toHaveBeenCalled();
    expect(component.isEditing.set).not.toHaveBeenCalled();
  });
});