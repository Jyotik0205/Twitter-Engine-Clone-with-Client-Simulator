#if INTERACTIVE
#r @"bin\MCD\Debug\netcoreapp3.1\Akka.dll"
#r @"bin\MCD\Debug\netcoreapp3.1\Akka.Remote.dll"
#r @"bin\MCD\Debug\netcoreapp3.1\Akka.Fsharp.dll"
#endif

open System
open System.Data
open System.Threading
open System.Security.Cryptography
open Akka.Actor
open Akka.Configuration
open Akka.FSharp
open System.Collections.Concurrent
//Inputs
let numUsers=fsi.CommandLineArgs.[1]|> int;
let cycles=fsi.CommandLineArgs.[2]|> int;

//let numUsers=1000;

let mutable bflag=true
let mutable mflag=true
let mutable nodesreached=0

let rand=System.Random()
type tweet =
    { From: int;
      Mention: int;
      Hashtag: string;
      Message: string 
       }

type UserMessage=
    |Tweet
    |Stop
type EngineMessage=
    |MTweet of tweet
    |Search of int
    |Searchbymention of int
    |Searchbyhash of string
    |Done
    |ReTweet of tweet
    |GetSubscribers of int
    |GetRequests
type ChildMessage=
    |Send
type NodeMessage=
    |Add of int*string
    |RouteTable of string[]*int*string
    |PrintRouteTable
    |Forward of string
    |LeafAdd of System.Collections.Generic.List<string>*System.Collections.Generic.List<string>*string
    |Addme of String*string[]
type ReaderMessage=
    |ReadTweet of int
    |ReadTweetbytag of string
    |ReadTweetbymention of int

let system=ActorSystem.Create("twitter")


//Sample data Table declared but not used due to lack of thread safety 
let tweets = new DataTable()
let dc1 = new DataColumn("FROM")
let dc2 = new DataColumn("Message")
let dc3 = new DataColumn("Mention")
let dc4 = new DataColumn("HashTag")
tweets.Columns.Add(dc1)
tweets.Columns.Add(dc2)
tweets.Columns.Add(dc3)
tweets.Columns.Add(dc4)

// USer Simulation Begins
let hashtaglist=new System.Collections.Generic.List<string>()
hashtaglist.Add("#sports")
hashtaglist.Add("#cooking")
hashtaglist.Add("#travel")
hashtaglist.Add("#election")
hashtaglist.Add("#weather")
hashtaglist.Add("#coffee")
hashtaglist.Add("#food")


let spawn_printer system1 name=
  let mutable state=0
  let mutable c=0
  let childname="child"+string(name)
  let username="User"+string(name)
  let mutable loggedin=false;
  let parent=system.ActorSelection("akka://twitter/user/twitterengine")
  //let mutable subsicriptionTable:System.Collections.Generic.List<int>;
  //let task=parent.Ask(GetSubscribers name)
  let task = parent <? GetSubscribers name
  let subsicriptionTable:System.Collections.Generic.List<int>=Async.RunSynchronously(task)
  //printfn "I am %d and subs size is:%d" name subsicriptionTable.Count
  let childactor=spawn system1 childname<|
                     fun mailbox->
                        let rec loop()=
                            actor{
                               let! msg=mailbox.Receive()
                               match msg with
                               |Send ->  let parent=system.ActorSelection("akka://twitter/user/twitterengine")
                                         //printfn "I am in %d" name
                                         while state<cycles do
                                             while c<10 do
                                                 let a=rand.Next(1,numUsers)
                                                 let t=rand.Next(1,7)
                                                 c<-c+1
                                                 let tweet={From= name ;Mention=a;Hashtag=hashtaglist.[t];Message= "sdfdsfdsf" }
                                                 Akka.Dispatch.ActorTaskScheduler.RunTask(fun()->
                                                 async{
                                                      parent.Tell(MTweet tweet)
                                                      
                                                      do! Async.Sleep (100/name)
                                                      
                                                 }|>Async.StartAsTask :> Threading.Tasks.Task)
                                             c<-0
                                             while c<3 do
                                                 let a=rand.Next(1,numUsers)
                                                 c<-c+1
                                                 let t=rand.Next(1,7)
                                                 let res=parent.Ask(Search a)|>Async.AwaitTask
                                                 //parent.Tell(Search a)
                                                 parent.Tell(Searchbymention a)
                                                 parent.Tell(Searchbyhash hashtaglist.[t])
                                             c<-0
                                             let t=rand.Next(1,7)
                                             let a=rand.Next(1,numUsers)
                                             let retweet={From= name ;Mention=a;Hashtag=hashtaglist.[t];Message= "sdfdsfdsf" }
                                             parent.Tell(MTweet retweet)
                                             loggedin<-false
                                             Akka.Dispatch.ActorTaskScheduler.RunTask(fun()->
                                                 async{

                                                      do! Async.Sleep (1000/name)
                                                      
                                                 }|>Async.StartAsTask :> Threading.Tasks.Task)
                                             
                                             state<-state+1
                                             let subs=subsicriptionTable
                                             for e in subs do
                                               parent.Tell(Search e)
                                         state<-0
                                         parent.Tell(Done)
                                        //  let parent=system.ActorSelection("akka://twitter/user/twitterengine")
                                        //  
                                        //    
                                        //     
                                        //     
                                        //     Thread.Sleep(10/name)
                                        //     
                                         return()
                               return! loop()
                                   }
                        loop()
  spawn system1 username<|
         fun mailbox->
             
             let rec loop()=
                actor{
                   let! msg=mailbox.Receive()
                   match msg with
                   |Tweet ->    
                                let child=system.ActorSelection("akka://twitter/user/child"+string(name))
                                child.Tell(Send)
                                return()
                   |Stop ->     state<-5
                   return! loop() 
                }
             loop()




// User (Client side) Simulation Build Ends


//Server Side Build
let tweetconcHash=new ConcurrentDictionary<string,Collections.Generic.List<tweet>>();
let tweetconcMent=new ConcurrentDictionary<int,Collections.Generic.List<tweet>>();
let tweetconcFrom=new ConcurrentDictionary<int,Collections.Generic.List<tweet>>();


let spawn_reader system name=
  let mutable state: int64=0L
  spawn system ("Reader"+string(name))<|
         fun mailbox->
             let mutable loggedin=false;
             let rec loop()=
                actor{
                   let! msg=mailbox.Receive()
                   match msg with
                   |ReadTweet  id -> //let qstr=String.Format("Mention = '{0}'",id);
                                     // let k=tweets.Select(qstr).TryTake()
                                      tweetconcFrom.TryGetValue(id)|>ignore 
                                      mailbox.Sender().Tell(Done) 
                                      return()
                   |ReadTweetbytag hashtag-> tweetconcHash.TryGetValue(hashtag)|>ignore      
                                             mailbox.Sender().Tell(Done) 
                                             return()
                   |ReadTweetbymention id -> tweetconcMent.TryGetValue(id)|>ignore      
                                             mailbox.Sender().Tell(Done) 
                                             return()
                   return! loop() 
                }
             loop()



let mutable unrec=0
let mutable numrequests=0
let controllerActor= spawn system "twitterengine" <| fun mailbox->
              
               let mutable currreader=1
               let subsicriptionTable=new System.Collections.Generic.List<System.Collections.Generic.List<int>>()
//let followingView=new System.Collections.Generic.List<System.Collections.Generic.List<int>>()
               for i=1 to numUsers|>int do
                   let subs=new System.Collections.Generic.List<int>()
                   for j=1 to (numUsers-1)/i do 
                      subs.Add(j)
                   subsicriptionTable.Add(subs)
              
               let ReaderList =  
                   [1..(numUsers/2)]
                      |> List.map(fun id-> spawn_reader mailbox id)
               let rec loop()=
                   actor{
                   let! msg=mailbox.Receive()
                  // printfn "From Parent %A" msg
                   match msg with
                   |MTweet data->  //tweets.Rows.Add(data.From,data.Mention,data.Hashtag,data.Message)|>ignore
                                   //printfn "I am in Parent from:%d" data.From
                                   numrequests<-numrequests+1
                                   if tweetconcHash.ContainsKey(data.Hashtag) then
                                         let l1=tweetconcHash.TryGetValue(data.Hashtag)
                                         let (k,b)=l1;
                                         //printfn "Boolean:%b and List:%A" k b
                                         b.Add(data)
                                         while not(tweetconcHash.TryUpdate(data.Hashtag,b,b)) do
                                            //printfn "OOps"
                                            Async.Sleep(0)|>ignore
                                         //printfn "After first If:%d" tweetconcHash.Count
                                   else
                                        let ltemp=new Collections.Generic.List<tweet>()
                                        ltemp.Add(data)
                                        while not(tweetconcHash.TryAdd(data.Hashtag,ltemp)) do
                                            // printfn "OOps"
                                             Async.Sleep(0)|>ignore
                                       // printfn "After first else:%d" tweetconcHash.Count

                                   if tweetconcMent.ContainsKey(data.Mention) then
                                         let l1=tweetconcMent.TryGetValue(data.Mention)
                                         let (k,b)=l1;
                                         b.Add(data)
                                         while not(tweetconcMent.TryUpdate(data.Mention,b,b)) do
                                            Async.Sleep(0)|>ignore
                                        // printfn "After second if:%d" tweetconcMent.Count
                                   else
                                        let ltemp=new Collections.Generic.List<tweet>()
                                        ltemp.Add(data)
                                        while not(tweetconcMent.TryAdd(data.Mention,ltemp)) do
                                             Async.Sleep(0)|>ignore
                                        //printfn "After second else:%d" tweetconcMent.Count
                                   if tweetconcFrom.ContainsKey(data.From) then
                                         let l1=tweetconcFrom.TryGetValue(data.From)
                                         let (k,b)=l1;
                                         b.Add(data)
                                         while not(tweetconcFrom.TryUpdate(data.From,b,b)) do
                                            Async.Sleep(0)|>ignore
                                         //printfn "After third if:%d" tweetconcFrom.Count
                                   else
                                        let ltemp=new Collections.Generic.List<tweet>()
                                        ltemp.Add(data)
                                        while not(tweetconcFrom.TryAdd(data.From,ltemp)) do
                                             Async.Sleep(0)|>ignore
                                        //printfn "After third else:%d" tweetconcFrom.Count
                                   return()
                   |ReTweet data ->  //tweets.Rows.Add(data.From,data.Mention,data.Hashtag,data.Message)|>ignore
                                   //printfn "I am in Parent from:%d" data.From
                                   numrequests<-numrequests+1
                                   if tweetconcHash.ContainsKey(data.Hashtag) then
                                         let l1=tweetconcHash.TryGetValue(data.Hashtag)
                                         let (k,b)=l1;
                                         //printfn "Boolean:%b and List:%A" k b
                                         b.Add(data)
                                         while not(tweetconcHash.TryUpdate(data.Hashtag,b,b)) do
                                            //printfn "OOps"
                                            Async.Sleep(0)|>ignore
                                         //printfn "After first If:%d" tweetconcHash.Count
                                   else
                                        let ltemp=new Collections.Generic.List<tweet>()
                                        ltemp.Add(data)
                                        while not(tweetconcHash.TryAdd(data.Hashtag,ltemp)) do
                                            // printfn "OOps"
                                             Async.Sleep(0)|>ignore
                                       // printfn "After first else:%d" tweetconcHash.Count

                                   if tweetconcMent.ContainsKey(data.Mention) then
                                         let l1=tweetconcMent.TryGetValue(data.Mention)
                                         let (k,b)=l1;
                                         b.Add(data)
                                         while not(tweetconcMent.TryUpdate(data.Mention,b,b)) do
                                            Async.Sleep(0)|>ignore
                                        // printfn "After second if:%d" tweetconcMent.Count
                                   else
                                        let ltemp=new Collections.Generic.List<tweet>()
                                        ltemp.Add(data)
                                        while not(tweetconcMent.TryAdd(data.Mention,ltemp)) do
                                             Async.Sleep(0)|>ignore
                                        //printfn "After second else:%d" tweetconcMent.Count
                                   if tweetconcFrom.ContainsKey(data.From) then
                                         let l1=tweetconcFrom.TryGetValue(data.From)
                                         let (k,b)=l1;
                                         b.Add(data)
                                         while not(tweetconcFrom.TryUpdate(data.From,b,b)) do
                                            Async.Sleep(0)|>ignore
                                         //printfn "After third if:%d" tweetconcFrom.Count
                                   else
                                        let ltemp=new Collections.Generic.List<tweet>()
                                        ltemp.Add(data)
                                        while not(tweetconcFrom.TryAdd(data.From,ltemp)) do
                                             Async.Sleep(0)|>ignore
                                        //printfn "After third else:%d" tweetconcFrom.Count
                                   return()
                   |Search id-> let reader=system.ActorSelection("akka://twitter/user/twitterengine/Reader"+string(currreader))
                                reader.Tell(ReadTweet id)
                                currreader<-currreader+1
                                if currreader>(numUsers/2) then
                                    currreader<-1
                                numrequests<-numrequests+1
                                return()
                   |Searchbyhash hashtag->  let reader=system.ActorSelection("akka://twitter/user/twitterengine/Reader"+string(currreader))
                                            reader.Tell(ReadTweetbytag hashtag)
                                            currreader<-currreader+1
                                            if currreader>(numUsers/2) then
                                                currreader<-1
                                            numrequests<-numrequests+1
                                            return()
                   |Searchbymention id->   let reader=system.ActorSelection("akka://twitter/user/twitterengine/Reader"+string(currreader))
                                           reader.Tell(ReadTweetbymention id)
                                           currreader<-currreader+1
                                           if currreader>(numUsers/2) then
                                            currreader<-1
                                           numrequests<-numrequests+1
                                           return()
                   |GetSubscribers id->    let slist=subsicriptionTable.[id-1]
                                           let User=mailbox.Sender()
                                           User<!slist
                                                                   
                   |Done ->     unrec<-unrec+1    
                                return() 
                   |GetRequests-> printfn "Requests Received: %d" numrequests
                   return! loop()
                 }
               loop()

let ActorList =   [1..numUsers]
                      |> List.map(fun id-> spawn_printer system id)
let parent=system.ActorSelection("akka://twitter/user/twitterengine")
printfn "Server Built...Press Enter to start User simulatiuon.."
Console.ReadLine()
printfn "Logging in and sending Tweets..."


//User Simulation Starts
let stopWatch = System.Diagnostics.Stopwatch.StartNew()
stopWatch.Start()
let begintime= stopWatch.ElapsedMilliseconds
#time
for i=1 to numUsers do
  let User=system.ActorSelection("akka://twitter/user/User"+string(i))
  User.Tell(Tweet)
while unrec<>numUsers do
    Async.Sleep(1)|>ignore
#time
let time=stopWatch.ElapsedMilliseconds-begintime
printfn "Time Taken to converge: %i ms\n" time
printfn "Total Requests:%d" (numrequests|>int64)
printfn "Requests per second:%f" (((numrequests|>float)*1000.0)/(time|>float))

