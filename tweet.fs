namespace Proj1ParikshyaKim
open System
open Akka.Actor
open Akka.FSharp
module tweet=
  type tweet =
    { From: int
      Mention: int
      Hashtag: string
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

  type ReaderMessage=
      |ReadTweet of int
      |ReadTweetbytag of string
      |ReadTweetbymention of int
 // let system=ActorSystem.Create("TwitterEngine")




  