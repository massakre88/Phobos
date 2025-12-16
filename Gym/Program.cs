using Phobos.Entities;

namespace Gym;

public static class Program
{
    public static void Main(string[] args)
    {
        var s1 = new Squad(1);
        var s2 = new Squad(2);
        var s3 = new Squad(1);
        Console.WriteLine($"{s1} == {s2}: {s1 == s2} {s1} == {s3}:  {s1 == s3}");
    }
}