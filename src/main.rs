extern crate discord;

use discord::*;
use discord::model::*;
use std::collections::HashMap;
use std::collections::HashSet;

fn stringifigication(
    room: ChannelId,
    author: &User,
    content: String,
    attachments: Vec<Attachment>,
) -> String {
    let mut content_list = Vec::new();
    if !content.is_empty() {
        content_list.push(content);
    }
    for attachment in attachments {
        content_list.push(attachment.proxy_url.clone());
    }
    return format!(
        "AUTO: Deleted message by {} in {}: {}",
        author.mention(),
        room.mention(),
        content_list.join("\n")
    );
}

fn init_chans(ready: ReadyEvent, source_server_id: ServerId) -> HashSet<u64> {
    let mut valid_channels = HashSet::new();
    for server in ready.servers {
        if let PossibleServer::Online(server) = server {
            if server.id == source_server_id {
                for channel in server.channels {
                    valid_channels.insert(channel.id.0);
                }
            }
        }
    }
    valid_channels
}

fn on_event(
    discord: &Discord,
    event: Event,
    valid_channels: &mut HashSet<u64>,
    map: &mut HashMap<u64, String>,
    source_server_id: ServerId,
    dest_channel_id: ChannelId,
) {
    match event {
        Event::ChannelCreate(Channel::Public(PublicChannel { id, server_id, .. })) => {
            if server_id == source_server_id {
                valid_channels.insert(id.0);
            }
        }
        Event::MessageCreate(Message {
                                 id,
                                 channel_id,
                                 author,
                                 content,
                                 attachments,
                                 ..
                             }) |
        Event::MessageUpdate {
            id,
            channel_id,
            author: Some(author),
            content: Some(content),
            attachments: Some(attachments),
            ..
        } => {
            if valid_channels.contains(&channel_id.0) {
                let strnk = stringifigication(channel_id, &author, content, attachments);
                map.insert(id.0, strnk);
            }
        }
        Event::MessageDelete { message_id, .. } => {
            if let Some(msg) = map.get(&message_id.0) {
                match discord.send_message(dest_channel_id, msg, "", false) {
                    Ok(msg) => println!("Sent: {}", msg.content),
                    Err(err) => println!("Error: {}", err),
                }
            }
        }
        _ => (),
    }
}

fn run(token: &str, source_server_id: ServerId, dest_channel_id: ChannelId) -> discord::Result<()> {
    let discord = Discord::from_user_token(token)?;
    let (mut connection, ready) = discord.connect()?;
    let mut valid_channels = init_chans(ready, source_server_id);
    if valid_channels.is_empty() {
        return Err(Error::Other("Specified server not found"));
    }
    let mut map = HashMap::new();
    println!("Listening!");
    loop {
        let event = connection.recv_event()?;
        on_event(
            &discord,
            event,
            &mut valid_channels,
            &mut map,
            source_server_id,
            dest_channel_id,
        );
    }
    // Ok(())
}

fn main() {
    let args = std::env::args().collect::<Vec<_>>();
    if args.len() != 4 {
        println!("Usage: ./cmd token source_server_id dest_channel_id");
        std::process::exit(1);
    }
    let token = &args[1];
    let source_server_id = ServerId(args[2].parse().unwrap());
    let dest_channel_id = ChannelId(args[3].parse().unwrap());
    if let Err(err) = run(token, source_server_id, dest_channel_id) {
        println!("{}", err);
        std::process::exit(1);
    }
}
