import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { form, FormField, required } from '@angular/forms/signals';
import { expectAccessible } from '../../../testing/expect-accessible';
import { FieldError } from './field-error';

@Component({
  imports: [FieldError, FormField],
  template: `
    <label for="name">Name</label>
    <input id="name" aria-describedby="name-error" [formField]="nameForm.name" />
    <app-field-error id="name-error" [field]="nameForm.name" />
  `,
})
class Host {
  protected readonly nameForm = form(signal({ name: '' }), (path) => {
    required(path.name, { message: 'Name is required.' });
  });
}

describe('FieldError', () => {
  it('stays empty until the field is touched', async () => {
    const fixture = TestBed.createComponent(Host);
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('app-field-error').textContent.trim()).toBe('');
    await expectAccessible(fixture.nativeElement);
  });

  it('shows the validation message once the field is touched', async () => {
    const fixture = TestBed.createComponent(Host);
    await fixture.whenStable();

    fixture.nativeElement.querySelector('input').dispatchEvent(new Event('blur'));
    await fixture.whenStable();

    expect(fixture.nativeElement.querySelector('app-field-error').textContent.trim()).toBe(
      'Name is required.',
    );
    await expectAccessible(fixture.nativeElement);
  });
});
