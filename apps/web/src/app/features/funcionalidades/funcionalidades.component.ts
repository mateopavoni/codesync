import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { PublicHeaderComponent } from '../../shared/components/public-header/public-header.component';
import { PublicFooterComponent } from '../../shared/components/public-footer/public-footer.component';

@Component({
  selector: 'app-funcionalidades',
  standalone: true,
  imports: [RouterLink, PublicHeaderComponent, PublicFooterComponent],
  templateUrl: './funcionalidades.component.html',
  styleUrl: './funcionalidades.component.css',
})
export class FuncionalidadesComponent {}
