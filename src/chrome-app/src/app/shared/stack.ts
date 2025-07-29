export class Stack<T> {
  private items: T[] = [];

  push(item: T): void {
    this.items = this.items.filter((i) => i !== item);
    this.items.push(item);
  }

  pop(): T | undefined {
    return this.items.pop();
  }

  remove(item: T): void {
    this.items = this.items.filter((i) => i !== item);
  }

  peek(): T | undefined {
    return this.items[this.items.length - 1];
  }

  isEmpty(): boolean {
    return this.items.length === 0;
  }

  clear(): void {
    this.items = [];
  }

  size(): number {
    return this.items.length;
  }
}
