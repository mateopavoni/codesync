import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PublicHeaderComponent } from '../../shared/components/public-header/public-header.component';
import { PublicFooterComponent } from '../../shared/components/public-footer/public-footer.component';

@Component({
  selector: 'app-acerca-de',
  standalone: true,
  imports: [RouterLink, PublicHeaderComponent, PublicFooterComponent],
  templateUrl: './acerca-de.component.html',
  styleUrl: './acerca-de.component.css',
})
export class AcercaDeComponent {}
