unsafe class Combiner
{
    public static T Combine<T>(Func<T, T, T> combinator, T left, T right) =>
        combinator(left, right);

    public static unsafe T UnsafeCombine<T>(delegate*<T, T, T> combinator, T left, T right)
        where T : unmanaged =>
        combinator(left, right);

    public unsafe void run()
    {
        int product = 0;

        static int localMultiply(int x, int y) => x * y;

        product = UnsafeCombine(&localMultiply, 3, 4);
        Console.WriteLine(product);
    }
}