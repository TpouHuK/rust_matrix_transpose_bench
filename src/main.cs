using System.Diagnostics; 
using System; 
using System.Threading; 
 
var rand = new Random(); 
 
var sizes = new[] { 100, 1000, 10_000, 50_000 }; 
 
foreach (var n in sizes)  
{ 
    var matrix = new float[n, n]; 
    for (var i = 0; i < n; i++)  
    { 
        for (var j = 0; j < n; j++) 
        { 
            matrix[i, j] = rand.Next(10); 
        } 
    } 
 
    Console.WriteLine("Measuring the time it takes to transpose "  
                          + $"a {n} by {n} matrix in the main thread..."); 
    var sw = new Stopwatch(); 
    sw.Start(); 
 
    for (var i = 0; i < n; i++) 
    { 
        for (var j = 0; j < n - 1 - i; j++) { 
            (matrix[i, j], matrix[n - 1 - j, n - 1 - i]) = (matrix[n - 1 - j, n - 1 - i], matrix[i, j]); 
        } 
    } 
 
    sw.Stop(); 
    Console.WriteLine($"Time taken: {(float) sw.Elapsed.Microseconds / 1_000} ms."); 
 
    for (var threadCount = 2; threadCount <= 3; threadCount++) 
    { 
        sw.Restart(); 
 
        long totalCount = (long) n * (n - 1) / 2; 
        long count = totalCount / threadCount; 
        var threads = new Thread[threadCount]; 
        for (var i = 0; i < threadCount; i++) 
        { 
            var begin = i * count; 
            if (i == threadCount - 1) 
            { 
                count = totalCount - (threadCount - 1) * count; 
            } 
 
            Console.WriteLine($"Begin: {begin}, count: {count}."); 
            var tws = new ThreadWithState(matrix, begin, count); 
            threads[i] = new Thread(tws.ThreadProc); 
            threads[i].Start(); 
        } 
 
        for (var i = 0; i < threadCount; i++)  
        { 
            threads[i].Join(); 
        } 
 
        sw.Stop(); 
        Console.WriteLine($"Dividing into {threadCount} threads and executing took " 
                          + $"{(float) sw.Elapsed.Microseconds / 1_000} ms."); 
    } 
} 
 
public class ThreadWithState 
{ 
    private float[,] matrix; 
    private int n; 
    private long begin, count; 
 
    public ThreadWithState(float[,] matrix, long begin, long count) 
    { 
        this.matrix = matrix; 
        this.n = matrix.GetLength(0); 
        this.begin = begin; 
        this.count = count; 
    } 
 
    public void ThreadProc()  
    { 
        var iBegin = begin / n; 
        var jBegin = begin % n; 
 
        for (var j = jBegin; j < n - 1 - iBegin; j++)  
        { 
            (matrix[iBegin, j], matrix[n - 1 - j, n - 1 - iBegin]) = (matrix[n - 1 - j, n - 1 - iBegin], matrix[iBegin, j]); 
            count--; 
        } 
 
        for (var i = iBegin + 1; count > 0; i++) 
        { 
            for (var j = 0; j < n - 1 - i; j++)  
            { 
                (matrix[i, j], matrix[n - 1 - j, n - 1 - i]) = (matrix[n - 1 - j, n - 1 - i], matrix[i, j]); 
                count--; 
            } 
        } 
    } 
}
