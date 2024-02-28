import argparse, pickle, random, sys, time
import util_common, util_custom, util_generator, util_mkiii, util_reach, util_solvers
import json
import jsonpickle
from json import JSONEncoder


WEIGHT_PATTERNS       = 10000
WEIGHT_COUNTS         =     1



COUNTS_SCALE_HALF     = (0.5, 1.5)
COUNTS_SCALE_ZERO     = (0.0, 1e10)



def scheme2output(scheme_info, tag_level, game_level, solver, randomize, weight_patterns, weight_counts, counts_scale, reach_setup, mkiii_setup, custom_constraints, show_path_tiles):
    si = scheme_info

    rows = len(tag_level)
    cols = len(tag_level[0])

    for tag_row, game_row in zip(tag_level, game_level):
        util_common.check(len(tag_row) == len(game_row) == cols, 'row length mismatch')
        for tag, game in zip(tag_row, game_row):
            util_common.check(game != util_common.VOID_TEXT, 'void game')
            util_common.check(game in si.game_to_tag_to_tiles, 'unrecognized game ' + game)
            util_common.check(tag == util_common.VOID_TEXT or tag in si.game_to_tag_to_tiles[game], 'unrecognized tag ' + tag + ' for game ' + game)

    print('using solver', solver.get_id())

    if mkiii_setup is not None:
        gen = util_mkiii.GeneratorMKIII(solver, randomize, rows, cols, si, tag_level, game_level)
    else:
        gen = util_generator.Generator(solver, randomize, rows, cols, si, tag_level, game_level)

    util_common.timer_section('add tile rules')
    gen.add_rules_tiles()

    if si.pattern_info is not None and weight_patterns != 0:
        util_common.timer_section('add pattern rules')
        gen.add_rules_patterns(weight_patterns)

    if si.count_info is not None and weight_counts != 0:
        util_common.timer_section('add count rules')
        lo, hi = counts_scale
        gen.add_rules_counts(False, lo, hi, weight_counts) # TODO? (si.tile_to_text is not None)

    if reach_setup is not None:
        util_common.timer_section('add reachability rules')
        gen.add_rules_reachability(util_reach.get_reach_info(rows, cols, reach_setup, si))

    if mkiii_setup is not None:
        util_common.timer_section('add mkiii rules')
        gen.add_rules_mkiii(util_mkiii.get_example_info(mkiii_setup))

    if custom_constraints and len(custom_constraints) > 0:
        util_common.timer_section('add custom')
        for custom_constraint in custom_constraints:
            custom_constraint.add(gen)

    util_common.timer_section('solve')

    result = None
    if gen.solve():
        util_common.timer_section('create output')
        result = gen.get_result()
        util_common.print_result_info(result, False)

    util_common.timer_section(None)

    return result



if __name__ == '__main__':
    util_common.timer_start()

    parser = argparse.ArgumentParser(description='Create output from scheme.')

    #parser.add_argument('--outfile', required=True, type=str, help='Output file (without extension, which will be added).')
    #parser.add_argument('--schemefile', required=True, type=str, help='Input scheme file.')

    parser.add_argument('--tagfile', type=str, help='Input tag file.')
    parser.add_argument('--gamefile', type=str, help='Input game file.')
    #parser.add_argument('--size', type=int, nargs=2, help='Level size (if no tag or game file provided.')

    parser.add_argument('--randomize', type=int, help='Randomize based on given number.')
    parser.add_argument('--show-path-tiles', action='store_true', help='Show path in tiles.')

    parser.add_argument('--solver', type=str, nargs='+', choices=util_solvers.SOLVER_LIST, default=[util_solvers.SOLVER_PYSAT_RC2], help='Solver name, from: ' + ','.join(util_solvers.SOLVER_LIST) + '.')
    parser.add_argument('--solver-portfolio-timeout', type=int, help='Force use of portfolio with given timeout (even for single solver).')

    parser.add_argument('--soft-patterns', action='store_true', help='Make patterns soft constraints.')
    parser.add_argument('--no-patterns', action='store_true', help='Don\'t use pattern rules, even if present.')
    parser.add_argument('--zero-counts', action='store_true', help='Only use counts to prevent tiles not occuring in region.')
    parser.add_argument('--no-counts', action='store_true', help='Don\'t use tile count rules, even if present.')

    parser.add_argument('--reach-move', type=str, nargs='+', default=None, help='Use reachability move rules, from: ' + ','.join(util_reach.RMOVE_LIST) + '.')
    parser.add_argument('--reach-wrap-cols', action='store_true', help='Wrap columns in reachability.')
    parser.add_argument('--reach-goal', type=str, nargs='+', default=None, help='Use reachability goals, from: ' + ','.join(util_reach.RGOAL_DICT.keys()) + ', plus meta.')
    parser.add_argument('--reach-open-zelda', action='store_true', help='Use Zelda open tiles.')

    parser.add_argument('--mkiii-example', type=str, choices=util_mkiii.EXAMPLES, help='MKIII example name, from: ' + ','.join(util_mkiii.EXAMPLES) + '.')
    parser.add_argument('--mkiii-layers', type=int, help='MKIII number of layers.')

    parser.add_argument('--custom', type=str, nargs='+', action='append', help='Constraints on output, from: ' + ','.join(util_custom.CUST_LIST) + ', plus options.')

    parser.add_argument('--compress', action='store_true', help='Compress output.')
    parser.add_argument('--result-only', action='store_true', help='Only save result file.')

    parser.add_argument('--quiet', action='store_true', help='Reduce output.')

    args = parser.parse_args()

    args.outfile = "output_image"
    args.schemefile = "scheme.json"
    args.size = [15, 15]

    if args.quiet:
        sys.stdout = open(os.devnull, 'w')

    if len(args.solver) == 1 and not args.solver_portfolio_timeout:
        solver = util_solvers.solver_id_to_solver(args.solver[0])
    else:
        solver = util_solvers.PortfolioSolver(args.solver, args.solver_portfolio_timeout)

    with open(args.schemefile, 'r') as f:
        scheme_info = jsonpickle.decode(f.read()) #pickle.load(f)
        for game in scheme_info.pattern_info.game_to_patterns:
            scheme_info.pattern_info.game_to_patterns[game] = util_common.evaluate_dictionary_keys(scheme_info.pattern_info.game_to_patterns[game], 4, 0)
        scheme_info.count_info.divs_to_game_to_tag_to_tile_count = util_common.evaluate_dictionary_keys(scheme_info.count_info.divs_to_game_to_tag_to_tile_count, 1, 0)
        for key1 in scheme_info.count_info.divs_to_game_to_tag_to_tile_count:
            for key2 in scheme_info.count_info.divs_to_game_to_tag_to_tile_count[key1]:
                for key3 in scheme_info.count_info.divs_to_game_to_tag_to_tile_count[key1][key2]:
                    scheme_info.count_info.divs_to_game_to_tag_to_tile_count[key1][key2][key3] = util_common.evaluate_dictionary_keys(scheme_info.count_info.divs_to_game_to_tag_to_tile_count[key1][key2][key3], 1, 0)

        scheme_info.tileset.tile_ids = util_common.evaluate_dictionary_keys(scheme_info.tileset.tile_ids, 1, 0)
        scheme_info.tileset.tile_to_image = util_common.evaluate_dictionary_keys(scheme_info.tileset.tile_to_image, 1, 0)
        for key1 in scheme_info.game_to_tag_to_tiles:
            for key2 in scheme_info.game_to_tag_to_tiles[key1]:
                    scheme_info.game_to_tag_to_tiles[key1][key2] = util_common.evaluate_dictionary_keys(scheme_info.game_to_tag_to_tiles[key1][key2], 1, 0)

    if args.size:
        if args.tagfile or args.gamefile:
            parser.error('cannot use --size with --tagfile or --gamefile')

        tag_level = util_common.make_grid(args.size[0], args.size[1], util_common.DEFAULT_TEXT)
        game_level = util_common.make_grid(args.size[0], args.size[1], util_common.DEFAULT_TEXT)

    elif args.tagfile or args.gamefile:
        if args.size:
            parser.error('cannot use --size with --tagfile or --gamefile')

        if args.tagfile and args.gamefile:
            tag_level = util_common.read_text_level(args.tagfile)
            game_level = util_common.read_text_level(args.gamefile)
        elif args.tagfile:
            tag_level = util_common.read_text_level(args.tagfile)
            game_level = util_common.make_grid(len(tag_level), len(tag_level[0]), util_common.DEFAULT_TEXT)
        elif args.gamefile:
            game_level = util_common.read_text_level(args.gamefile)
            tag_level = util_common.make_grid(len(game_level), len(game_level[0]), util_common.DEFAULT_TEXT)

    else:
        parser.error('must use --size, --tagfile or --gamefile')



    reach_setup = None

    if args.reach_move or args.reach_goal:
        if not args.reach_move or not args.reach_goal:
            parser.error('must use --reach-move and --reach-goal together')

        reach_setup = util_common.ReachabilitySetup()
        reach_setup.wrap_cols = False
        reach_setup.open_text = util_common.OPEN_TEXT

        reach_setup.game_to_move = util_common.arg_list_to_dict_options(parser, '--reach-move', args.reach_move, util_reach.RMOVE_LIST)

        if args.reach_goal[0] not in util_reach.RGOAL_DICT:
            parser.error('--reach-goal[0] must be in ' + ','.join(util_reach.RGOAL_DICT.key()))
        reach_setup.goal_loc = args.reach_goal[0]

        if len(args.reach_goal[1:]) != util_reach.RGOAL_DICT[reach_setup.goal_loc]:
            parser.error('--reach-goal[1:] must be length ' + str(util_reach.RGOAL_DICT[reach_setup.goal_loc]))

        reach_setup.goal_params = []
        for rg in args.reach_goal[1:]:
            if not rg.isnumeric():
                parser.error('--reach-goal[1:] must all be integer')
            reach_setup.goal_params.append(int(rg))

    if args.reach_open_zelda:
        if not reach_setup:
            parser.error('cannot specify --reach-open-zelda without other reach args')
        reach_setup.open_text = util_common.OPEN_TEXT_ZELDA

    if args.reach_wrap_cols:
        if not reach_setup:
            parser.error('cannot specify --reach-wrap-cols without other reach args')
        reach_setup.wrap_cols = True



    mkiii_setup = None

    if args.mkiii_example or args.mkiii_layers:
        if not args.mkiii_example or not args.mkiii_layers:
            parser.error('must use --mkiii-example and --mkiii-layers together')

        mkiii_setup = util_mkiii.MKIIISetup()
        mkiii_setup.example = args.mkiii_example
        mkiii_setup.layers = args.mkiii_layers



    custom_constraints = []

    if args.custom:
        for cust_args in args.custom:
            custom_constraints.append(util_custom.args_to_custom(cust_args[0], cust_args[1:]))


    if args.no_patterns:
        weight_patterns = 0
    elif args.soft_patterns:
        weight_patterns = WEIGHT_PATTERNS
    else:
        weight_patterns = None

    if args.no_counts:
        weight_counts = 0
    else:
        weight_counts = WEIGHT_COUNTS

    if args.zero_counts:
        counts_scale = COUNTS_SCALE_ZERO
    else:
        counts_scale = COUNTS_SCALE_HALF

    result_info = scheme2output(scheme_info, tag_level, game_level, solver, args.randomize, weight_patterns, weight_counts, counts_scale, reach_setup, mkiii_setup, custom_constraints, args.show_path_tiles)
    if result_info:
        util_common.save_result_info(result_info, args.outfile, args.compress, args.result_only)
        util_common.exit_solution_found()
    else:
        util_common.exit_solution_not_found()
