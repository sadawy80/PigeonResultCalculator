import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ThemeService } from './core/services/services';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  template: `<router-outlet />`,
  styles: [`
    :host { display: block; min-height: 100vh; background: var(--pr-bg); color: var(--pr-text); }
  `]
})
export class AppComponent implements OnInit {
  private theme = inject(ThemeService);

  ngOnInit() {
    this.theme.loadSavedTheme();
    this.theme.loadThemesFromApi();
  }
}
