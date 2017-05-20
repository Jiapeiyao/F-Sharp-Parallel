#load "data.fs"
#r "System.dll"
#r "Akka.dll"
#r "Akka.FSharp.dll"
#r "System.Core.dll"
//#r "FSharp.Core.dll"

open System
open System.IO
open System.Threading
open System.Diagnostics
open System.Threading.Tasks
open System.Collections.Generic
open System.Collections.Concurrent
open System.Linq
open Microsoft.FSharp.Control
open Data
open Akka
open Akka.Actor
open Akka.Configuration
open Akka.FSharp

// Define your library scripting code here

let modulus m n = ((m % n)+n)%n

let verbosity = ref 0
let duration f = 
    let timer = Stopwatch()
    timer.Start()
    let res = f()
    timer.Stop()
    printfn "$$$ duration: %i ms" 
            timer.ElapsedMilliseconds
    res

//Sequential implementation (/SEQ)
module SEQ =
    let processLines (M:int[][]) =
        let row = Array.length M
        let len = Array.length M.[0]
        let newl = Array.create (len+2) 0
        for j in 0 .. row-1 do
            let l2 = Array.concat [ [|0|]; M.[j] ; [|0|]]
            for i in 1 .. len do 
                l2.[i] <- newl.[i]+ l2.[i]
            if (!verbosity=1) then
                printf "%A 0" j
                for k in 1 .. len do
                    printf " %A" l2.[k]
                printfn ""
            for i in 1 .. len do 
                newl.[i] <- (max l2.[i] (max l2.[i-1] l2.[i+1]))
        Array.max(newl) 
    // let rec processLines lastLine list2d fprocessLine row=
    //     match list2d with
    //     | line :: tail -> 
    //         let tmpline = (Array.map2(+) line (fprocessLine lastLine))
    //         if (!verbosity=1) then
    //             printf "%A 0" row
    //             for entity in tmpline do
    //                 printf " %A" entity
    //             printfn ""
    //         processLines tmpline tail fprocessLine (row+1)
    //     | [] -> lastLine
    // let getMax lineLength array2d fprocessLine =
    //     let initialLine = Array.create lineLength 0
    //     let finalLine = processLines initialLine (Array.toList array2d) fprocessLine 0
    //     Array.max(finalLine)
    // let rec getMaxOf3inLine lastNum line = 
    //     match line with 
    //         | (a: int)::(b: int) :: tail -> max lastNum (max a b) :: getMaxOf3inLine a (b :: tail)
    //         | [c] -> [max lastNum c]
    //         | _ -> []
    // let processLine line = List.toArray (getMaxOf3inLine 0 (Array.toList line))
    let Output (M: int[][]) k =
        printfn "$$$ sequential (no range)"
        let lineLength = Array.length M.[0]
        let res = duration(fun () -> processLines M)
        printfn "$$$ %A max_sequential" res
        0
    // let test_seq =
    //     let M = 
    //         [|
    //             [|0; 0; 4; 0; 0; 3|]
    //             [|0; 4; 0; 0; 3; 0|]
    //             [|4; 0; 0; 3; 0; 0|]
    //             [|0; 0; 3; 0; 0; 1|]
    //             [|0; 3; 0; 0; 1; 0|]
    //             [|3; 0; 0; 1; 0; 0|]
    //         |]
    //     let lineLength = Array.length M.[0]
    //     (Output_seq M 0 1)

//Task based naive parallel implementation (/PAR-NAIVE)
module PAR_NAIVE = 
    let processLines (M:int[][]) =
        let row = Array.length M
        let len = Array.length M.[0]
        let newl = Array.create (len+2) 0
        for j in 0 .. row-1 do
            let l2 = Array.concat [ [|0|]; M.[j] ; [|0|]]
            Parallel.For(1, len+1, fun i -> 
                l2.[i] <- newl.[i]+ l2.[i]
            ) |> ignore
            if (!verbosity=1) then
                printf "%A 0" j
                for k in 1 .. len do
                    printf " %A" l2.[k]
                printfn ""
            Parallel.For(1, len+1 , fun i -> 
                newl.[i] <- (max l2.[i] (max l2.[i-1] l2.[i+1]))
            ) |> ignore
        Array.max(newl) 
    // let test =
    //     let m = 
    //         [|
    //             [|0; 0; 4; 0; 0; 3|]
    //             [|0; 4; 0; 0; 3; 0|]
    //             [|4; 0; 0; 3; 0; 0|]
    //             [|0; 0; 3; 0; 0; 1|]
    //             [|0; 3; 0; 0; 1; 0|]
    //             [|3; 0; 0; 1; 0; 0|]
    //         |]
    //     processLines m
        
    let Output (M: int[][]) k =
        printfn "$$$ naive_parallel (not range)"
        let res = duration(fun () -> processLines M)
        printfn "$$$ %A max_naive_parallel" res
        0

//Task based parallel implementation with explicit range partitioning (/PAR-RANGE)
module PAR_RANGE = 
    let processLines (M:int[][]) ranges =
        let row = Array.length M
        let len = Array.length M.[0]
        let chunk = if (ranges=0) then len
                    else if (ranges > len) then 1
                    else (floor (float len)/(float ranges)) |> int
        let newl = Array.create (len+2) 0
        for j in 0 .. row-1 do
            let l2 = Array.concat [ [|0|]; M.[j] ; [|0|]]
            for (low, high) in (Partitioner.Create (1, len+1, chunk)).AsParallel().AsEnumerable() do
                Parallel.For(low, high , fun i -> (l2.[i] <- newl.[i]+ l2.[i])) |> ignore
                if (!verbosity=1) then
                    printf "%A %A" j low
                    for k in low .. high-1 do
                        printf " %A" l2.[k]
                    printfn ""
            for (low, high) in (Partitioner.Create (1, len+1, chunk)).AsParallel().AsEnumerable() do
                Parallel.For(low, high , fun i -> newl.[i] <- (max l2.[i] (max l2.[i-1] l2.[i+1]))) |> ignore
        Array.max(newl) 
    let Output (M: int[][]) k =
        printfn "$$$ parallel_range: %A" k
        let lineLength = Array.length M.[0]
        let res = duration(fun () -> processLines M k)
        printfn "$$$ %A max_parallel_range" res
        res
    // let test =
    //     let M = 
    //         [|
    //             [|0; 0; 4; 0; 0; 3|]
    //             [|0; 4; 0; 0; 3; 0|]
    //             [|4; 0; 0; 3; 0; 0|]
    //             [|0; 0; 3; 0; 0; 1|]
    //             [|0; 3; 0; 0; 1; 0|]
    //             [|3; 0; 0; 1; 0; 0|]
    //         |]
    //     Output M 10


//Async based parallel implementation with explicit range partitioning (/ASYNC-RANGE)
module ASYNC_RANGE = 
    let processLines (M:int[][]) ranges =
        let row = Array.length M
        let len = Array.length M.[0]
        let chunk = if (ranges=0) then len
                    else if (ranges > len) then 1
                    else (floor (float len)/(float ranges)) |> int
        let newl = Array.create (len+2) 0
        for j in 0 .. row-1 do
            let l2 = Array.concat [ [|0|]; M.[j] ; [|0|]]
            for (low, high) in (Partitioner.Create (1, len+1, chunk)).AsParallel().AsEnumerable() do
                let taskList = [ for i in low .. high-1 -> async { l2.[i] <- newl.[i]+ l2.[i] } ]
                taskList |> Async.Parallel |> Async.RunSynchronously |> ignore
                if (!verbosity=1) then
                    printf "a%A b%A" j low
                    for k in low .. high-1 do
                        printf " c%A" l2.[k]
                    printfn ""
            for (low, high) in (Partitioner.Create (1, len+1, chunk)).AsParallel().AsEnumerable() do
                let taskList = [ for i in low .. high-1 -> async { newl.[i] <- (max l2.[i] (max l2.[i-1] l2.[i+1])) } ]
                taskList |> Async.Parallel |> Async.RunSynchronously |> ignore
        Array.max(newl) 
    let Output (M: int[][]) k =
        printfn "$$$ range_asynchronous: %A" k
        let lineLength = Array.length M.[0]
        let res = duration(fun () ->  processLines M k)
        printfn "$$$ %A max_range_asynchronous" res
        res
    // let test =
    //     //let M = Data.getData fname
    //     let M = 
    //         [|
    //             [|0; 0; 4; 0; 0; 3|]
    //             [|0; 4; 0; 0; 3; 0|]
    //             [|4; 0; 0; 3; 0; 0|]
    //             [|0; 0; 3; 0; 0; 1|]
    //             [|0; 3; 0; 0; 1; 0|]
    //             [|3; 0; 0; 1; 0; 0|]
    //         |]
    //     let lineLength = Array.length M.[0]
    //     Output M 10


//Actor based parallel implementation with explicit range partitioning (/MAILBOX-RANGE)
module MAILBOX_RANGE =
    let processLine (array2d:int[][]) ranges : int=
        let numOfLines = Array.length array2d
        let len = Array.length array2d.[0]
        let chunk = if (ranges<1) then len
                    else if (ranges >= len) then 1
                    else if (len%ranges=0) then len/ranges
                    else (len/ranges + 1)
        // let chunk = if (ranges=0) then len
        //             else if (ranges >= len) then 1
        //             else (floor (float len)/(float ranges)) |> int
        let newl = Array.create len 0
        //let partitionNum = int (ceil ((float len)/(float chunk)))
        let partitionNum = ranges
        let agents = Array.zeroCreate<MailboxProcessor<int list>> partitionNum
        let results = Array.create partitionNum 0
        let count = ref partitionNum
        let actors_started = TaskCompletionSource<bool> ()
        let actors_completed = TaskCompletionSource<bool> ()
        for i in 0 .. (partitionNum-1) do
            if Interlocked.Decrement (count) = 0 then 
                    count := partitionNum
                    actors_started.SetResult true
            let low = i * chunk
            let high = if i=partitionNum-1 then len else (i+1)*chunk
            let partitionLength = high - low
            agents.[i] <- MailboxProcessor.Start(fun inbox->  async {
                Thread.Sleep (1 * (partitionNum-i) )
                let lineNumber = ref 0
                let processerArray = Array.create partitionLength 0
                while true do
                    if !lineNumber=numOfLines then
                        results.[i] <- (Array.max processerArray)
                        //printfn "Z: %A" !count
                        if Interlocked.Decrement(count) = 0 then 
                            actors_completed.SetResult true
                        ()
                    else
                        //read and add the numbers from the nexy line of M
                        if !verbosity=1 then printf "%A %A" lineNumber low
                        for k in 0 .. partitionLength-1 do
                            processerArray.[k] <- array2d.[!lineNumber].[low + k] + processerArray.[k]
                            if !verbosity=1 then printf " %A" processerArray.[k]
                        if !verbosity=1 then printfn ""
                        //Thread.Sleep 10
                        if (partitionLength-1)>=0 then
                            //send and receive message to find max of neighbors and self
                            let mutable valuel = 0
                            let mutable valuer = 0
                            if (i>0) then 
                                agents.[i-1].Post [i; !lineNumber; processerArray.[0]]
                            else
                                agents.[partitionNum-1].Post [0; !lineNumber; 0]
                            if (i<partitionNum-1) then
                                agents.[i+1].Post [i; !lineNumber; processerArray.[partitionLength-1]]
                            else 
                                agents.[0].Post [-1; !lineNumber; 0]
                            let! m1 = inbox.Receive()
                            let! m2 = inbox.Receive()
                            if m1.Item(0)=i-1 || m1.Item(0)=(-1) then valuel<-m1.Item(2)
                            elif m1.Item(0)=i+1 || m1.Item(0)=0 then valuer<-m1.Item(2)
                            if m2.Item(0)=i-1 || m2.Item(0)=(-1) then valuel<-m2.Item(2)
                            elif m2.Item(0)=i+1 || m2.Item(0)=0 then valuer<-m2.Item(2)
                            let tmpArray = Array.concat [[|valuel|]; Array.copy processerArray; [|valuer|]]
                            for k in 0 .. partitionLength-1 do
                                processerArray.[k] <- max tmpArray.[k] (max tmpArray.[k+1] tmpArray.[k+2])
                }
                )
        actors_completed.Task.Wait ()
        Array.max results
    
    

    let Output (M: int[][]) k =
        printfn "$$$ mailbox_range: %A" k
        let lineLength = Array.length M.[0]
        let res = duration(fun () -> processLine M k)
        printfn "$$$ %A max_mailbox_range" res
        res
    // let test : int = 
    //     let M = 
    //         [|
    //             [|0; 0; 4; 0; 0; 3|]
    //             [|0; 4; 0; 0; 3; 0|]
    //             [|4; 0; 0; 3; 0; 0|]
    //             [|0; 0; 3; 0; 0; 1|]
    //             [|0; 3; 0; 0; 1; 0|]
    //             [|3; 0; 0; 1; 0; 0|]
    //         |]
    //     (Output M 3)

        

//Actor based parallel implementation with explicit range partitioning (/AKKA-RANGE)
module AKKA_RANGE = 
    let tid () = Thread.CurrentThread.ManagedThreadId
    let config = ConfigurationFactory.ParseString <|  @"
    akka {
        suppress-json-serializer-warning = on
    }
    "
    let system = ActorSystem.Create ("FSharp-Akka", config)
    let mre = new AutoResetEvent (false)

    let processLine array2d ranges=
        let numOfLines = Array.length array2d
        let len = Array.length array2d.[0]
        let chunk = if (ranges<1) then len
                    else if (ranges >= len) then 1
                    else if (len%ranges=0) then len/ranges
                    else (len/ranges + 1)
        let newl = Array.create len 0
        //let partitionNum = int (ceil ((float len)/(float chunk)))
        let partitionNum = ranges
        let agents = Array.zeroCreate<IActorRef> partitionNum
        let results = Array.create partitionNum 0
        let count = ref partitionNum
        let actors_completed = TaskCompletionSource<bool> ()
        for i in 0 .. (partitionNum-1) do
            if Interlocked.Decrement (count) = 0 then 
                    count := partitionNum
            let low = i * chunk
            let high = if i=partitionNum-1 then len else (i+1)*chunk
            let partitionLength = high - low
            agents.[i] <- spawn system (String.concat "" ["agent";string i]) <| fun (inbox:Actor<int list>) ->actor {
                Thread.Sleep (1 * (partitionNum-i) )
                let lineNumber = ref 0
                let processerArray = Array.create partitionLength 0
                while true do
                    if !lineNumber=numOfLines then
                        results.[i] <- (Array.max processerArray)
                        if Interlocked.Decrement(count) = 0 then 
                            actors_completed.SetResult true
                        ()
                    else
                        //read and add the numbers from the nexy line of M
                        if !verbosity=1 then printf "%A %A" !lineNumber low
                        for k in 0 .. partitionLength-1 do
                            processerArray.[k] <- array2d.[!lineNumber].[low + k] + processerArray.[k]
                            if !verbosity=1 then printf " %A" processerArray.[k]
                        if !verbosity=1 then printfn ""

                        //printfn "(%A %A)" i processerArray.[0]
                        if (partitionLength-1)>=0 then
                            //send and receive message to find max of neighbors and self
                            let mutable valuel = 0
                            let mutable valuer = 0
                            if (i>0) then 
                                agents.[i-1].Tell [i; !lineNumber; processerArray.[0]]
                                //printfn "to left %A" i
                            else
                                agents.[partitionNum-1].Tell [0; !lineNumber; 0]
                            //Thread.Sleep 1
                            if (i<partitionNum-1) then
                                agents.[i+1].Tell [i; !lineNumber; processerArray.[partitionLength-1]]
                            else 
                                agents.[0].Tell [-1; !lineNumber; 0]
                            //Thread.Sleep 1
                            let! m1 = inbox.Receive()
                            let! m2 = inbox.Receive()
                            if m1.Item(0)=i-1 || m1.Item(0)=(-1) then valuel<-m1.Item(2)
                            elif m1.Item(0)=i+1 || m1.Item(0)=0 then valuer<-m1.Item(2)
                            if m2.Item(0)=i-1 || m2.Item(0)=(-1) then valuel<-m2.Item(2)
                            elif m2.Item(0)=i+1 || m2.Item(0)=0 then valuer<-m2.Item(2)
                            let tmpArray = Array.concat [[|valuel|]; Array.copy processerArray; [|valuer|]]
                            for k in 0 .. partitionLength-1 do
                                processerArray.[k] <- max tmpArray.[k] (max tmpArray.[k+1] tmpArray.[k+2])
                            // if Interlocked.Decrement(count) = 0 then 
                            //     count := partitionNum
                            //     actors_completed.SetResult true
                            // actors_completed.Task.Wait ()
                            // actors_completed.SetResult false
                }
        actors_completed.Task.Wait ()
        // for i in results do
        //     printf "%A " i
        // printfn ""
        Array.max results

   

    let Output (M: int[][]) k =
        printfn "$$$ Akka_range: %A" k
        let res = duration(fun () -> processLine M k)
        printfn "$$$ %A max_Akka_range" res
        res
    // let test = 
    //     let M = 
    //         [|
    //             [|0; 0; 4; 0; 0; 3|]
    //             [|0; 4; 0; 0; 3; 0|]
    //             [|4; 0; 0; 3; 0; 0|]
    //             [|0; 0; 3; 0; 0; 1|]
    //             [|0; 3; 0; 0; 1; 0|]
    //             [|3; 0; 0; 1; 0; 0|]
    //         |]
    //     Output M 6

        
[<EntryPoint >]
let main args = 
    let run fname alg k v =
        let M:int[][] = Data.getData fname
        match alg with
        | "/SEQ" -> SEQ.Output M k
        | "/PAR-NAÏVE"|"/PAR-NAIVE" -> PAR_NAIVE.Output M k
        | "/PAR-RANGE" -> PAR_RANGE.Output M k
        | "/ASYNC-RANGE" -> ASYNC_RANGE.Output M k
        | "/MAILBOX-RANGE" -> MAILBOX_RANGE.Output M k
        | "/AKKA-RANGE" -> AKKA_RANGE.Output M k
        | _ -> 0
    let res = match args with
                |[|fname; alg; k; v|]->
                    verbosity := (v |> int)
                    run fname alg (k|>int) v
                |_->
                    printfn("Fail! Usage: A1.EXE fname /alg k v ")
                    0
    //printfn "alg: %A" res
    0


//main [|"m-100-3000-lr.txt"; "/ASYNC-RANGE"; "1"; "0" |]









// printfn "Result %A" (SEQ.Run)
//printfn "Result %A" (PAR_NAIVE.test)
// printfn "Result %A" (PAR_RANGE.test)
// printfn "Result %A" (ASYNC_RANGE.test)
// printfn "Result %A" (MAILBOX_RANGE.Run)
// printfn "Result %A" (AKKA_RANGE.test)

        // let agents = [ for i in 0 .. len-1 -> MailboxProcessor<Message>.Start( fun inbox -> 
        //     let rec loop n = 
        //         async { 
        //             try
        //                 let! (message, replyChannel) = inbox.Receive (1000);
        //                 //if ()
        //                 //printfn "    [%d] n=%d: %s" (tid())  n message
        //                 replyChannel.Reply(sprintf "n=%d: %s" n message)
        //                 do! loop (n + 1)
        //             with
        //              | :? TimeoutException -> 
        //             printfn "*** [%d] n=%d: timeout" (tid()) n
        //         }
        //     loop 0
        //     )
        // ]

        // let ask question =
        //     let reply = 
        //         agent.PostAndReply (
        //             (fun replyChannel -> (question, replyChannel)), 1000)
        //     printfn "... [%d] %s" (tid()) reply

        // ["the"; "quick"; "brown"; "fox"] 
        // |> List.map ask
        // |> ignore