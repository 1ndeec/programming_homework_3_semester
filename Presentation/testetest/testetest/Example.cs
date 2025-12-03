internal unsafe class Example
{
    internal unsafe struct Buffer
    {
        public fixed char fixedBuffer[128];
    }

    public Buffer buffer = default;

    public void AccessEmbeddedArray()
    {
        unsafe
        {
            // Pin the buffer so the GC can't move it
            fixed (char* charPtr = buffer.fixedBuffer)
            {
                *charPtr = 'A';
            }

            // Safe access via indexer
            char c = buffer.fixedBuffer[0];
            Console.WriteLine(c);

            // Modify via indexer
            buffer.fixedBuffer[0] = 'B';
            Console.WriteLine(buffer.fixedBuffer[0]);
        }
    }
}



