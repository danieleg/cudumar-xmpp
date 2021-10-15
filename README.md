# cudumar-xmpp
<h2>Cudumar-xmpp, un nuovo client gratuito e leggero conforme allo standard jabber/xmpp</h2>
Oggi presentiamo un innovativo software per accedere ai network XMPP: cudumar-xmpp. Gratuito, leggero, 100% conforme allo standard XMPP. Il nome buffo deriva dalle origini geografiche degli ideatori, friulani DOC e con uno spiccato amore per le verdure. Infatti cudumar in friulano significa cetriolo, un nome decisamente buffo da assegnare ad un programma!

<br />
<br />
<img src='https://danieletech.files.wordpress.com/2011/11/cudumar-v0-0-2.png' />
<i>cudumar-xmpp versione 0.0.2</i>
<br />
<br />
Ho avuto il piacere di incontrare di persona Daniele Tenero, uno degli ideatori.

<br />
<h4>La domanda di rito: come nasce questo nome… “cudumar”?</h4>
A me piaceva “cudumar & verzutin”. Hai mai fatto una minestra di cetrioli e verze? Spettacolare… dovresti provarla. Fai bollire due cetrioli grossi a piacere in un litro d’acqua, dagli solo una lavata sotto acqua corrente ma non togliere la buccia! E’ la parte più buona, i cetrioli non vanno pelati. Aggiungi metà verza e lascia riposare per qualche minuto. Frulla il tutto con un mixer ad immersione, aggiungi sale, pepe se vuoi anche una cipolla che fa sempre bene e ti leccherai i baffi.
Volevamo un nome simpatico, che ricordasse la nostra friulanità, l’amore per le verdure, lo stile di vita semplice e casereccio. E così è anche cudumar-xmpp, semplice e anche un po’ contadino, senza tanti fronzoli un po’ come la gente friulana, che mantiene ancora un forte legame con la terra e la natura.

<h4>Parlaci di questo progetto. Perché è nato?</h4>
Inizialmente voleva solo essere un esercizio di stile, ne più ne meno. Ci interessiamo da tempo di protocolli e standard aperti sia per lavoro che per passione, è una filosofia che ci piace e intriga moltissimo. Tutto è nato dalla necessità di approfondire come lavora il server Jabber di Google Talk per valutarne possibili interazioni con client esterni. Soprattutto per quanto riguarda la trasmissione audio vocale, le chiamate VoIP, i flussi video. Un bel lavoro di reverse engineering che ci ha portati a conoscere nel dettaglio lo standard xmpp, le sue estensioni ma anche molte variazioni “fuori standard”. E’ nato per passione, perchè a noi piace impastarci il cervello, tenerci sempre in movimento, e questi risultati ci ripagano con grande soddisfazione.

<h4>Dopo un approccio filosofico buttiamoci sul tecnico. Quali tecnologie adotta cudumar-xmpp?</h4>
Le ultime e più innovative sul mercato: WPF .Net Framework 4. Con WPF si semplifica enormemente tutta l’analisi e lo sviluppo dell’interfaccia utente. L’utilizzo della versione 4 del .Net Framework permette di sfruttare tutti i vantaggi di un linguaggio di alto livello e di particolari funzionalità. Esempio: LINQ to XML ci ha permesso di semplificare notevolmente l’implementazione del protocollo XMPP, che per chi non lo sapesse è basato su XML.

<h4>Un progetto nato da poco ma già sulla bocca di tutti, se ne parla molto nelle comunità online. Quali sono i piani di sviluppo futuri?</h4>
Abbiamo puntato molto sul supporto alla chat di Facebook, con l’implementazione di un nuovo meccanismo SASL (DIGEST-MD5) per l’autenticazione ai servizi facebook. Attualmente, con questa nuova release, cudumar-xmpp è in grado di collegarsi a qualsiasi server jabber che supporti SASL PLAIN / TLS / SASL DIGEST-MD5. Abbiamo in programma di supportare altri meccanismi di autenticazione per estendere il supporto alla totalità dei servizi jabber in rete. Oltre questo aspetto tecnico ci preme il restiling grafico del logo, stiamo valutando diverse proposte grafiche da persone altamente fantasiose.

<h4>Un cetriolo parlante quindi?</h4>
Potrebbe essere.

<h4>Ultima domanda, ma non la meno importante: dove ti troviamo in rete?</h4>
Esiste un blog del progetto dove è possible restare sempre aggiornati sulle ultime novità e ultime releases, una pagina su facebook che rende il tutto molto social, la pagina di progetto per gli sviluppatori ospitata su google code dove è possibile contribuire e scaricare l’ultima versione del programma.
