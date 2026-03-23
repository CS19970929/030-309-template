ENTRY(Reset_Handler)

_estack = 0x20001B80;

MEMORY
{
  RAM_VECTOR (xrw) : ORIGIN = 0x20000000, LENGTH = 0x000000C0
  RAM        (xrw) : ORIGIN = 0x200000C0, LENGTH = 0x00001AC0
  RAM_TOP_RSVD (xrw) : ORIGIN = 0x20001B80, LENGTH = 0x00000480
  FLASH      (rx)  : ORIGIN = 0x08001C00, LENGTH = 0x0000D400
}

SECTIONS
{
  .isr_vector :
  {
    . = ALIGN(4);
    KEEP(*(.isr_vector))
    . = ALIGN(4);
  } > FLASH

  .text :
  {
    . = ALIGN(4);
    KEEP(*(.text.Reset_Handler))
    KEEP(*(.text.Default_Handler))
    *(.text*)
    *(.rodata*)
    . = ALIGN(4);
    _etext = .;
  } > FLASH

  _sidata = _etext;

  .data : AT(_sidata)
  {
    . = ALIGN(4);
    _sdata = .;
    *(.data*)
    . = ALIGN(4);
    _edata = .;
  } > RAM

  .bss :
  {
    . = ALIGN(4);
    _sbss = .;
    *(.bss*)
    *(COMMON)
    . = ALIGN(4);
    _ebss = .;
  } > RAM

  .RAMVectorTable (NOLOAD) :
  {
    . = ALIGN(4);
    KEEP(*(.RAMVectorTable))
    . = ALIGN(4);
  } > RAM_VECTOR

  ._user_heap_stack (NOLOAD):
  {
    . = ALIGN(8);
    . = . + @APP_HEAP_SIZE@;
    . = . + @APP_STACK_SIZE@;
    . = ALIGN(8);
  } > RAM

  /DISCARD/ :
  {
    *(.note*)
    *(.comment*)
    *(.eh_frame*)
    *(.ARM.extab*)
    *(.ARM.exidx*)
    *(.preinit_array*)
    *(.init_array*)
    *(.fini_array*)
  }
}
