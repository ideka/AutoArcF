open System
open System.Net
open System.IO
open System.Security.Cryptography
open System.Diagnostics

let localPath = "bin64/"
let localBakPath = "bin64/bak/"
let gameExeName = "Gw2-64.exe"

type Remote = {
    name:string;

    remotePath:string;
    checksumPath:string option;
    localName:string option;
}

let localNameOf (remote:Remote) =
    match remote.localName with
    | Some(value) -> value
    | None -> remote.remotePath.Split [|'/'|] |> Array.last

let updateRemote (remote:Remote) =
    let fullLocalPath = localPath + localNameOf remote

    let updateExists =
        printfn "Checking for %s updates..." remote.name
        if not <| File.Exists(fullLocalPath) then
            printfn "Local file not found, downloading..."
            true
        else
            use resp = WebRequest.Create(remote.remotePath).GetResponse():?>HttpWebResponse
            let lastModifiedRepr = resp.LastModified.ToString()
            if File.GetLastWriteTimeUtc(fullLocalPath) >= resp.LastModified then
                printfn "Up to date (%s)." lastModifiedRepr
                false
            else
                printfn "Downloading update (%s)..." lastModifiedRepr
                Directory.CreateDirectory(localBakPath) |> ignore
                File.Copy(fullLocalPath, localBakPath + localNameOf remote, true)
                true

    if updateExists then
        use wc = new WebClient()
        let file = wc.DownloadData(remote.remotePath)

        let checksumVerified =
            match remote.checksumPath with
            | None ->
                printfn "No checksum verification for %s." remote.name
                true
            | Some(checksumPath) ->
                use md5 = MD5.Create()
                let checksum = (wc.DownloadString(checksumPath).Split [|' '|] |> Array.head).Trim()
                let downloadedChecksum = BitConverter.ToString(md5.ComputeHash(file)).Replace("-", "").ToLower()
                let equal = checksum = downloadedChecksum
                printfn "%s == %s (%b)" checksum downloadedChecksum equal
                if not <| equal then
                    Console.ReadKey() |> ignore
                equal

        if checksumVerified then
            File.WriteAllBytes(fullLocalPath, file)

    printfn ""

[<EntryPoint>]
let main argv =
    updateRemote {
        name = "ArcDPS";
        remotePath = "https://www.deltaconnected.com/arcdps/x64/d3d9.dll";
        checksumPath = Some("https://www.deltaconnected.com/arcdps/x64/d3d9.dll.md5sum");
        localName = None;
    }

    updateRemote {
        name = "ArcDPS Build Templates";
        remotePath = "https://www.deltaconnected.com/arcdps/x64/buildtemplates/d3d9_arcdps_buildtemplates.dll";
        checksumPath = None;
        localName = None;
    }

    updateRemote {
        name = "ArcDPS Extras";
        remotePath = "https://www.deltaconnected.com/arcdps/x64/extras/d3d9_arcdps_extras.dll";
        checksumPath = None;
        localName = None;
    }

    updateRemote {
        name = "ArcDPS Mechanics";
        remotePath = "http://martionlabs.com/wp-content/uploads/d3d9_arcdps_mechanics.dll";
        checksumPath = Some("http://martionlabs.com/wp-content/uploads/d3d9_arcdps_mechanics.dll.md5sum");
        localName = None;
    }

    match Array.tryLast argv with
    | None -> Process.Start(gameExeName)
    | Some(arg) -> Process.Start(gameExeName, arg)
    |> ignore

    0
