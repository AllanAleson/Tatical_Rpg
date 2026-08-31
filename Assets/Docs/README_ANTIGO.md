# Tactical RPG

Projeto de RPG tático desenvolvido em Unity, atualmente focado na construção da base de combate em grid.

O objetivo é criar um sistema modular capaz de suportar múltiplas unidades, iniciativa, movimentação tática, combate, diferentes comportamentos de IA, habilidades, equipamentos e progressão de personagens.

---

## Estado atual

A base de combate já possui:

- múltiplas unidades Player e Enemy;
- seleção dinâmica de unidades;
- combate por iniciativa;
- Pontos de Movimento (PM);
- Pontos de Ação (PA);
- movimentação em grid;
- pathfinding;
- obstáculos e bloqueio por unidades;
- ataque básico;
- sistema de HP;
- estado de unidade desmaiada;
- UI de iniciativa;
- HP flutuante;
- preview de caminho;
- highlight de movimento e ataque;
- custo de movimento;
- IA modular para inimigos;
- IA corpo a corpo agressiva;
- prefabs básicos de unidades e UI.

---

# Estrutura atual

```text
Assets/
├── Prefabs/
│   ├── UI/
│   └── Units/
│
└── Scripts/
    ├── AI/
    │   ├── Behaviours/
    │   │   └── AggressiveMeleeAI.cs
    │   ├── EnemyAIBehaviour.cs
    │   └── EnemyBrain.cs
    │
    ├── Combat/
    │   └── CombatActions.cs
    │
    ├── Core/
    │   ├── TurnManager.cs
    │   └── UnitManager.cs
    │
    ├── Grid/
    │   ├── AttackHighLighter.cs
    │   ├── GridHover.cs
    │   ├── GridManager.cs
    │   ├── MovementHighlighter.cs
    │   ├── Pathfinding.cs
    │   └── PathPreview.cs
    │
    ├── Input/
    │   └── ClickManager.cs
    │
    ├── UI/
    │   ├── InitiativeSlot.cs
    │   ├── InitiativeUI.cs
    │   ├── MoveCostUI.cs
    │   ├── PlayerHUD.cs
    │   └── WorldHPText.cs
    │
    └── Units/
        ├── Movement/
        │   ├── PlayerMovement.cs
        │   └── UnitMovement.cs
        └── UnitStats.cs
```

---

# Unidades

Cada unidade utiliza `UnitStats` para armazenar seu estado de combate.

Atualmente são controlados dados como:

- Team (`Player` ou `Enemy`);
- HP máximo e atual;
- PM máximo e atual;
- PA máximo e atual;
- dano de ataque;
- alcance de ataque;
- custo de ataque;
- iniciativa base;
- iniciativa rolada;
- estado `isDowned`.

Unidades desmaiadas:

- permanecem registradas no combate;
- não podem agir;
- não podem ser alvo de ataques básicos;
- não bloqueiam o pathfinding;
- permanecem na ordem de iniciativa;
- podem voltar a agir na posição original da iniciativa caso sejam revividas.

---

# UnitManager

`UnitManager` é responsável pelo gerenciamento dinâmico das unidades.

As unidades se registram automaticamente no sistema.

O sistema permite:

- registrar unidades;
- remover unidades;
- selecionar unidades Player;
- obter Players vivos;
- obter Enemies vivos;
- suportar unidades criadas durante a execução.

O jogo não depende mais de referências fixas para um único `Cube` ou Player.

---

# Sistema de iniciativa

O combate utiliza uma ordem de iniciativa persistente.

No início do combate:

1. cada unidade viva realiza sua iniciativa;
2. o resultado é combinado com a iniciativa base;
3. as unidades são ordenadas;
4. a ordem permanece durante o combate.

Critérios de desempate:

1. maior iniciativa total;
2. maior iniciativa base;
3. desempate aleatório.

Unidades desmaiadas permanecem na lista, mas têm seus turnos ignorados enquanto estiverem incapacitadas.

`TurnManager` controla:

- início do combate;
- ordem de iniciativa;
- unidade atual;
- avanço dos turnos;
- reset de PA/PM da unidade atual.

Também disponibiliza eventos para outros sistemas:

- `OnInitiativeCreated`;
- `OnTurnStarted`.

---

# UI de iniciativa

A interface mostra a ordem atual das unidades.

A lista é apresentada de forma circular, começando pela unidade cujo turno está ativo.

Cada slot pode mostrar:

- nome;
- iniciativa rolada;
- equipe;
- estado atual;
- estado desmaiado.

A interface reage aos eventos do `TurnManager` e não depende de atualização constante por frame.

---

# Movimento

O movimento utiliza `UnitMovement` como base compartilhada.

`PlayerMovement` utiliza essa implementação mantendo compatibilidade com o sistema do jogador.

O movimento:

- consome PM;
- segue caminhos calculados pelo pathfinding;
- respeita obstáculos;
- respeita unidades vivas;
- não permite movimento para a própria célula.

---

# Pathfinding

`Pathfinding` recebe explicitamente a unidade que está tentando se mover.

Principais operações:

```text
FindPath(startPosition, targetX, targetZ, moverStats)

CanReach(startPosition, targetX, targetZ, moverStats)

IsBlocked(cell, ignoredObject)
```

O sistema:

- ignora o próprio objeto que está se movendo;
- considera objetos com tag `Obstacle`;
- considera unidades vivas como bloqueios;
- ignora unidades desmaiadas;
- impede caminhos inválidos;
- é compartilhado entre Player e IA.

---

# Preview e highlights

O jogo possui feedback visual para ações táticas.

### Movimento

- mostra células disponíveis;
- mostra preview do caminho;
- mostra custo `-X PM`;
- esconde caminhos inválidos;
- não mostra custo zero.

### Ataque

- mostra o alcance do ataque;
- utiliza a unidade atualmente selecionada.

Ao trocar de modo, highlights incompatíveis são removidos.

Modos atuais:

```text
None
Move
Attack
Skill
```

O modo `Skill` está reservado para o futuro sistema de habilidades.

---

# Combate

`CombatActions` concentra regras compartilhadas de combate.

Player e Enemy utilizam o mesmo sistema de ataque, evitando duplicação de regras.

Atualmente o ataque básico considera:

- PA disponível;
- custo do ataque;
- alcance;
- estado do atacante;
- estado do alvo;
- aplicação de dano.

Friendly fire ainda é permitido pelo ataque básico.

O sistema de combate será expandido posteriormente com rolagens, atributos, armas e habilidades.

---

# Inteligência Artificial

A IA utiliza uma arquitetura baseada em Strategy.

```text
EnemyBrain
    ↓
EnemyAIBehaviour
    ↓
Comportamento específico
```

`EnemyAIBehaviour` é um `ScriptableObject`, permitindo configurar diferentes comportamentos sem criar lógica de IA diretamente no `TurnManager`.

Comportamento implementado atualmente:

```text
AggressiveMeleeAI
```

---

## AggressiveMeleeAI

A IA corpo a corpo agressiva:

1. procura o Player vivo mais próximo;
2. verifica se pode atacar;
3. caso necessário, procura uma rota válida;
4. move-se utilizando PM;
5. consegue contornar obstáculos através do pathfinding;
6. ataca quando entra no alcance;
7. continua atacando enquanto possuir PA suficiente;
8. encerra automaticamente seu turno quando termina suas ações.

O comportamento utiliza:

- `UnitManager`;
- `Pathfinding`;
- `UnitMovement`;
- `CombatActions`;
- `TurnManager`.

Isso mantém decisão, movimento, combate e gerenciamento de turnos separados.

---

# Direção do sistema de personagens

Algumas decisões de design foram definidas para as próximas etapas.

## Atributos

A intenção atual é utilizar seis atributos:

```text
FOR - Força
DES - Destreza
CON - Constituição
INT - Inteligência
SAB - Sabedoria
CAR - Carisma
```

Os atributos utilizarão valores numéricos tradicionais de RPG, por exemplo:

```text
FOR 16 → modificador +3
DES 12 → modificador +1
```

A distribuição e progressão ainda serão balanceadas antes da implementação definitiva.

---

# Filosofia de classes

Uma decisão central do projeto é:

> A classe não determina tudo que um personagem pode fazer. Ela determina aquilo em que ele é especialmente treinado ou eficiente.

Exemplos:

- um Guerreiro pode utilizar um arco;
- um Arqueiro pode utilizar uma espada;
- um personagem não-mago pode utilizar um cajado que conceda uma magia;
- equipamentos podem conceder novas ações;
- atributos e treinamento determinam a eficiência dessas ações;
- talentos e especializações permitem extrair mais potencial de determinados estilos.

A intenção é evitar restrições artificiais como impedir completamente uma ação apenas por causa da classe.

---

# Talentos

O sistema de progressão planejado possui três categorias principais.

### Talentos Comuns

Disponíveis independentemente da classe.

Exemplos:

- Melhorar Atributo;
- aumento de resistência;
- novas proficiências;
- melhorias gerais do personagem.

### Talentos de Classe

Representam técnicas e vantagens ligadas à identidade da classe.

Exemplo para um Espadachim:

- Ataque Pesado;
- Bloqueio;
- Ripostar;
- Investida.

### Talentos de Especialização

São desbloqueados através de requisitos e combinações específicas.

Exemplo:

```text
Ataque Pesado
      +
Bloqueio
      ↓
Guarda da Lâmina
```

Nesse caso, o personagem poderia aprender a utilizar uma arma grande de forma defensiva.

Nem toda combinação de talentos necessariamente terá uma especialização própria.

O objetivo é permitir builds diferentes sem exigir uma quantidade excessiva de subclasses rígidas.

---

# PA e armas

O sistema de Pontos de Ação será expandido para que diferentes armas e ações possam possuir custos diferentes.

Exemplo conceitual:

```text
Faca
baixo custo de PA
baixo dano
alcance curto

Espadão
alto custo de PA
alto dano
alcance e técnicas diferentes

Arco
ataque à distância
utiliza atributos apropriados

Cajado mágico
pode conceder uma habilidade mágica própria
```

Assim, quantidade de ataques e eficiência durante o turno não dependem apenas da classe.

Talentos, equipamento e especialização poderão modificar a economia de PA.

Os valores definitivos ainda serão definidos através de testes e balanceamento.

---

# Próximas etapas

Os próximos sistemas planejados são:

1. implementar atributos;
2. adicionar rolagens de ataque com d20;
3. implementar Defesa;
4. criar dados de armas;
5. permitir diferentes custos de PA por arma/ação;
6. implementar diferentes alcances e tipos de ataque;
7. criar sistema genérico de habilidades;
8. criar estrutura de talentos;
9. adicionar talentos comuns, de classe e de especialização;
10. expandir a IA para utilizar novas ações e habilidades.

O desenvolvimento continuará de forma incremental, validando cada sistema antes de expandir o próximo.

---

# Status

A base atual já permite executar um combate tático simples entre múltiplas unidades Player e Enemy com:

```text
Iniciativa
    ↓
Seleção / IA
    ↓
Movimento
    ↓
Pathfinding
    ↓
Ataque
    ↓
Consumo de PA/PM
    ↓
Próximo turno
```

O foco atual é evoluir essa base para um sistema completo de RPG tático sem acoplar regras de personagem, armas, habilidades ou IA diretamente umas às outras.