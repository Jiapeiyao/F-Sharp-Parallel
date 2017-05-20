// ---
open System
open System.Collections.Concurrent
open System.Diagnostics
open System.IO
open System.Linq
open System.Threading
open System.Threading.Tasks
// ---

let PROCS = Environment.ProcessorCount

let print_procs () = 
    Console.Error.WriteLine (sprintf "... Environment.ProcessorCount: %d" PROCS)

let print_threadpool () = 
    let MinWorkers, MinIOs = ThreadPool.GetMinThreads ()
    let MaxWorkers, MaxIOs = ThreadPool.GetMaxThreads ()
    let AvailWorkers, AvailIOs = ThreadPool.GetAvailableThreads ()
    Console.Error.WriteLine (sprintf "... ThreadPool Min: %d,%d; Max: %d,%d; Avail: %d,%d" MinWorkers MinIOs MaxWorkers MaxIOs AvailWorkers AvailIOs)

// ---

let tid () = Thread.CurrentThread.ManagedThreadId

// ---

let default_partitions n =
    let W = 
        Partitioner.Create(1, n+1).AsParallel() // partitions start from 1, to skip left sentinel !
        |> Array.ofSeq |> Array.sort            // sort required for actors !
    W

let my_partitions n k =
    let k = if k <= n then k else n
    let s, k' = n / k, n % k
    let k'' = k - k'
    let h = k'*(s+1)
    let W = Array.init k (fun i -> 
        if i < k' 
        then (i*(s+1), (i+1)*(s+1))
        else (h+(i-k')*s, h+(i+1-k')*s))
    W |> Array.map (fun (low, high) -> (low+1, high+1)) // + 1 !

let create_partitions n k = 
    if k = 0 then default_partitions n
    else my_partitions n k

let print_partitions (W:(int*int)[]) =
    //Console.Error.WriteLine (sprintf "... partitions count: %d; first: %A; last: %A" W.Length W.[0] W.[w.Length-1])
    Console.Error.WriteLine (sprintf "... partitions")
    for i = 0 to W.Length-1 do
        Console.Error.WriteLine (sprintf "... %d %A %d" i W.[i] (snd W.[i] - fst W.[i]))

// ---
[<EntryPoint>]
let main (args:string[]) =
    try 
        //print_procs ()
        //print_threadpool ()
        
        Console.Error.WriteLine (sprintf "... args: %A" args)
        if args.Length <> 4 then failwith "command-line arguments"
        
        let logname = args.[0] 
        let N1 = Int32.Parse args.[1]
        let N2 = Int32.Parse args.[2]
        let k = Int32.Parse args.[3]
                
        // partitions start from 1, to skip left sentinel !
        // and are sorted, as required for actors !
        let W = 
            if k >= 0 then create_partitions N2 k 
            else [| (1, N2+1) |]  // + sentinel
        let K = (Array.length W)    

        let W' = set (W |> Array.map (fun (low, high1) -> (low-1, high1-1)))
        //W'.Dump "W'"
        let mutable werr = 0
        
        let R = Array.init N1 (fun i1 -> Array.zeroCreate<int> N2)
        
        use logstream = new StreamReader (logname)
        let mutable maxi1 = 0
        let mutable pari1 = 0
        let mutable maxlow = 0
        let mutable parlow = 0

        let rec loop () = 
            let line = logstream.ReadLine ()
            if line <> null then 
                if line.Trim() = "" then ()
                elif line.StartsWith ("//") then ()
                elif line.StartsWith ("...") then ()
                elif line.StartsWith ("$$$") then Console.Error.WriteLine (sprintf "%s" line)
                else 
                    //Console.Error.WriteLine (sprintf "%s" line)
                    let nums = 
                        line.Split (Array.empty<char>, StringSplitOptions.RemoveEmptyEntries)
                        |> Array.map (fun s -> Int32.Parse s)
                        |> Array.map (fun n -> if n >= 0 then n else failwith "negative number")
                    
                    let i1 = nums.[0]
                    let low = nums.[1]

                    if i1 < maxi1 then 
                        pari1 <- pari1 + 1
                    elif i1 > maxi1 then
                        maxi1 <- i1
                        maxlow <- low
                    
                    if low < maxlow then 
                        parlow <- parlow + 1
                    elif low > maxlow then
                        maxlow <- low
                    
                    let len = nums.Length - 2
                    let high1 = low + len 
                    if not (Set.contains (low, high1) W') then 
                        werr <- werr + 1
                        Console.Error.WriteLine (sprintf "*** %A" (low, high1))
                    Array.blit nums 2 R.[i1] low len
                loop ()
                
        loop ()
        
        Console.Error.WriteLine (sprintf "*** werr=%d, pari1=%d, parlow=%d" werr pari1 parlow)
        //(array2D R).Dump "R"
        
        R |> Seq.iter (fun row -> Console.Out.WriteLine (String.Join (" ", row |> Array.map (fun n -> n.ToString()))))
        0
    
    with
    | ex -> 
        Console.Error.WriteLine (sprintf "*** %s" ex.Message)
        Console.Out.WriteLine (sprintf "*** %s" ex.Message)
        1
        
// ---
// main (Environment.GetCommandLineArgs ()).[1..]
//main [| "log-m-100-3000-lr-PAR-RANGE-0-1.txt"; "100"; "3000"; "0"|] |> ignore
// ---