# Tactical RPG

Projeto de RPG tático por turnos desenvolvido em Unity e C#, com foco em combate estratégico em grid, movimentação baseada em pontos e sistemas modulares de combate e equipamento.

## Estado atual

O projeto já possui uma base funcional de combate tático, incluindo:

- Sistema de turnos e iniciativa
- Pontos de Movimento (PM) e Pontos de Ação (PA)
- Seleção de unidades
- Grid tático
- Movimento ortogonal e diagonal
- Pathfinding ponderado com A* e Dijkstra
- Custos reais de movimentação
- Obstáculos e prevenção de corner cutting
- Preview e destaque de movimentação
- Sistema de atributos
- Armas com alcance, custo de PA e dados de dano
- Armaduras e escudos
- Defesa e redução de dano
- Ataques críticos
- Friendly fire
- Linha de ataque bloqueada por obstáculos e unidades
- Sistema de unidades downed
- IA inimiga com movimentação baseada no menor custo real de rota
- Estrutura experimental de Q-Learning para testes de IA

## Pathfinding

O sistema de movimentação foi migrado de um BFS limitado pela quantidade de células para um sistema de busca ponderada.

O custo do caminho é separado da quantidade de células percorridas.

Exemplo:

- Movimento ortogonal: 1 PM
- Movimento diagonal: 2 PM
- Terrenos e conexões especiais poderão possuir custos próprios

O sistema utiliza:

- A* para navegação espacial convencional
- Dijkstra quando conexões especiais tornam a heurística espacial inadequada

A arquitetura já permite futuras conexões como:

- Portais
- Barcos
- Elevadores
- Passagens especiais
- Teleportes

## Combate

O sistema de combate possui validação centralizada de:

- Alcance
- Linha de ataque
- PA disponível
- Defesa
- Acerto crítico
- Dano
- Redução de dano

Armas com alcance maior que 1 não podem atacar através de paredes ou unidades vivas.

## Equipamentos

### Armas

As armas podem definir:

- Dados de dano
- Alcance
- Custo de PA
- Atributo utilizado no ataque
- Uso de uma ou duas mãos
- Compatibilidade com escudo

### Armaduras

Armaduras podem modificar:

- Defesa
- Redução de dano
- Uso de Destreza na defesa
- Iniciativa
- Pontos de Movimento

### Escudos

Escudos podem fornecer:

- Defesa
- Redução de dano
- Modificador de iniciativa
- Modificador de movimento

Os bônus só ficam ativos quando o equipamento atual permite o uso do escudo.

## IA

A IA inimiga não escolhe mais movimentos apenas pela distância geométrica até o alvo.

Ela considera o custo real da rota e pode aceitar movimentos que parecem piores imediatamente caso resultem em um caminho globalmente melhor.

A rota é recalculada a cada turno.

## Tecnologias

- Unity
- C#
- Git
- ScriptableObjects
- Pathfinding A*
- Dijkstra

## Estrutura de documentação

Documentação adicional e registros anteriores estão disponíveis em:

```text
Assets/Docs/