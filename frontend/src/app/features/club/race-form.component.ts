import { Component, inject, signal, OnInit } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators, FormArray } from '@angular/forms';
import { ApiService } from '../../core/services/api.service';
import { AuthService } from '../../core/services/services';

const WIND_DIRECTIONS = ['N','NE','E','SE','S','SW','W','NW'];

@Component({
  selector: 'app-race-form',
  standalone: true,
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './race-form.component.html',
  styleUrls: ['./race-form.component.scss']
})
export class RaceFormComponent implements OnInit {
  private fb     = inject(FormBuilder);
  private api    = inject(ApiService);
  private router = inject(Router);
  private route  = inject(ActivatedRoute);
  auth = inject(AuthService);

  windDirs = WIND_DIRECTIONS;
  isEdit   = signal(false);
  saving   = signal(false);
  error    = signal<string | null>(null);

  get clubId(): string { return this.auth.clubId() ?? ''; }

  form = this.fb.group({
    name:                 ['', Validators.required],
    description:          [''],
    scheduledReleaseTime: [''],
    releaseLocation:      ['', Validators.required],
    releaseLatitude:      [null as number | null, Validators.required],
    releaseLongitude:     [null as number | null, Validators.required],
    windSpeedKmh:         [null as number | null],
    windDirection:        [''],
    temperatureCelsius:   [null as number | null],
    categories:           this.fb.array([])
  });

  get categories() { return this.form.get('categories') as FormArray; }

  ngOnInit() {
    const id = this.route.snapshot.paramMap.get('id');
    if (id && id !== 'new') {
      this.isEdit.set(true);
      this.api.getRace(id).subscribe(r => {
        this.form.patchValue({
          name:                 r.name,
          description:          r.description ?? '',
          releaseLocation:      r.releaseLocation,
          releaseLatitude:      r.releaseLatitude,
          releaseLongitude:     r.releaseLongitude,
          windSpeedKmh:         r.windSpeedKmh ?? null,
          windDirection:        r.windDirection ?? '',
          temperatureCelsius:   r.temperatureCelsius ?? null,
        });
        r.categories.forEach(c => this.categories.push(
          this.fb.group({ name: [c.name], description: [c.description ?? ''] })
        ));
      });
    }
  }

  addCategory() {
    this.categories.push(this.fb.group({ name: ['', Validators.required], description: [''] }));
  }

  removeCategory(i: number) { this.categories.removeAt(i); }

  touched(field: string): boolean {
    const ctrl = this.form.get(field);
    return !!(ctrl?.invalid && ctrl?.touched);
  }

  submit() {
    if (this.form.invalid) { this.form.markAllAsTouched(); return; }
    this.saving.set(true);
    this.error.set(null);

    const v = this.form.value;
    const payload = {
      clubId:               this.clubId,
      name:                 v.name!,
      description:          v.description || undefined,
      scheduledReleaseTime: v.scheduledReleaseTime || undefined,
      releaseLocation:      v.releaseLocation!,
      releaseLatitude:      v.releaseLatitude!,
      releaseLongitude:     v.releaseLongitude!,
      windSpeedKmh:         v.windSpeedKmh ?? undefined,
      windDirection:        v.windDirection || undefined,
      temperatureCelsius:   v.temperatureCelsius ?? undefined,
      categories: (v.categories as any[]).map((c, i) => ({
        name: c.name, description: c.description || undefined, sortOrder: i
      }))
    } as Partial<import('../../core/models').Race> & { clubId: string; categories: any[] };

    const raceId = this.route.snapshot.paramMap.get('id');
    const req$ = this.isEdit() && raceId
      ? this.api.updateRace(raceId, payload)
      : this.api.createRace(payload);

    req$.subscribe({
      next: r => { this.saving.set(false); this.router.navigate(['/club/races', r.id]); },
      error: (e: any) => { this.error.set(e?.error?.message ?? 'Failed to save race.'); this.saving.set(false); }
    });
  }
}
