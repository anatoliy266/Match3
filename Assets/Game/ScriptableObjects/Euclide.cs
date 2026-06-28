public static class Euclide
{
    public static int Run(int totalCells)
    {
        if (totalCells <= 1) return 1;

        int candidate = (totalCells / 2) | 1;

        while (candidate > 1)
        {
            int a = candidate;
            int b = totalCells;

            while (b != 0)
            {
                int temp = b;
                b = a % b;
                a = temp;
            }

            if (a == 1)
            {
                return candidate;
            }

            candidate -= 2;
        }

        return 1;
    }
}