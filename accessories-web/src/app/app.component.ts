import { Component, OnInit, inject, ChangeDetectorRef } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ProductService, Product } from './services/product';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [FormsModule],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css'
})
export class AppComponent implements OnInit {
  products: Product[] = [];
  
  // Modal state management
  isModalOpen = false;
  isEditing = false;
  currentProduct: Product = { name: '', category: '', price: 0 };
  
  private productService = inject(ProductService);
  private cdr = inject(ChangeDetectorRef);

  ngOnInit(): void {
    this.loadProducts();
  }

  loadProducts(): void {
    this.productService.getProducts().subscribe({
      next: (data) => {
        this.products = data;
        this.cdr.detectChanges(); // Ensure UI updates reliably when data arrives
      },
      error: (err) => {
        console.error('Failed to load products from API:', err);
      }
    });
  }

  // --- Modal Logic ---

  openAddModal(): void {
    this.isEditing = false;
    this.currentProduct = { name: '', category: '', price: 0 }; // Reset form
    this.isModalOpen = true;
  }

  openEditModal(product: Product): void {
    this.isEditing = true;
    this.currentProduct = { ...product }; // Copy data so table doesn't change live while typing
    this.isModalOpen = true;
  }

  closeModal(): void {
    this.isModalOpen = false;
  }

  // --- CRUD Operations ---

  saveProduct(): void {
    if (this.isEditing && this.currentProduct.id) {
      this.productService.updateProduct(this.currentProduct.id, this.currentProduct).subscribe({
        next: () => {
          this.loadProducts(); 
          this.closeModal();   
        },
        error: (err) => {
          console.error('Failed to update product:', err);
          alert('There was an error updating the product.');
        }
      });
    } else {
      this.productService.createProduct(this.currentProduct).subscribe({
        next: () => {
          this.loadProducts();
          this.closeModal();
        },
        error: (err) => console.error('Failed to create product:', err)
      });
    }
  }

  deleteProduct(id: number | undefined): void {
    if (id && confirm('Are you sure you want to delete this product?')) {
      this.productService.deleteProduct(id).subscribe({
        next: () => {
          this.loadProducts();
        },
        error: (err) => console.error('Failed to delete product:', err)
      });
    }
  }
}